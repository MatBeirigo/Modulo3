using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Core.Models;

namespace Services;

public class BroadcastReceiverService : BackgroundService
{
    private readonly ILogger<BroadcastReceiverService> _logger;
    private readonly DataAggregationService _aggregationService;
    private readonly ConcurrentQueue<string> _tcpPacketQueue;
    private readonly ConcurrentQueue<string> _udpPacketQueue;
    private readonly CancellationTokenSource _cts;
    private const int TcpPort = 5555;
    private const int UdpPort = 5002;
    private const int ProcessorThreadCount = 4;

    public BroadcastReceiverService(ILogger<BroadcastReceiverService> logger, DataAggregationService aggregationService)
    {
        _logger = logger;
        _aggregationService = aggregationService;
        _tcpPacketQueue = new ConcurrentQueue<string>();
        _udpPacketQueue = new ConcurrentQueue<string>();
        _cts = new CancellationTokenSource();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BroadcastReceiverService iniciado");

        var tasks = new List<Task>
        {
            Task.Run(() => StartTcpListener(stoppingToken), stoppingToken),
            Task.Run(() => StartUdpListener(stoppingToken), stoppingToken)
        };

        for (int i = 0; i < ProcessorThreadCount; i++)
        {
            tasks.Add(Task.Run(() => ProcessTcpPackets(stoppingToken), stoppingToken));
            tasks.Add(Task.Run(() => ProcessUdpPackets(stoppingToken), stoppingToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task StartTcpListener(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Any, TcpPort);
        listener.Start();
        _logger.LogInformation("TCP Listener iniciado na porta {Port}", TcpPort);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken);
                _ = Task.Run(() => HandleTcpClient(client, cancellationToken), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TCP Listener encerrado");
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleTcpClient(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            var stream = client.GetStream();
            var buffer = new byte[8192];

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                    if (bytesRead == 0) break;

                    var packet = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (IsValidPacketFormat(packet, out var normalizedPacket))
                    {
                        _tcpPacketQueue.Enqueue(normalizedPacket);
                    }
                    else
                    {
                        _logger.LogWarning("Pacote TCP inválido recebido (formato incorreto). Pacote: {Packet}",
                            packet.Length > 200 ? packet[..200] + "..." : packet);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar cliente TCP");
            }
        }
    }

    private async Task StartUdpListener(CancellationToken cancellationToken)
    {
        using var udpClient = new UdpClient(UdpPort);
        _logger.LogInformation("UDP Listener iniciado na porta {Port}", UdpPort);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await udpClient.ReceiveAsync(cancellationToken);
                var packet = System.Text.Encoding.UTF8.GetString(result.Buffer);

                if (IsValidPacketFormat(packet, out var normalizedPacket))
                {
                    _udpPacketQueue.Enqueue(normalizedPacket);
                }
                else
                {
                    _logger.LogWarning("Pacote UDP inválido recebido (formato incorreto). Pacote: {Packet}",
                        packet.Length > 200 ? packet[..200] + "..." : packet);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("UDP Listener encerrado");
        }
    }

    private bool IsValidPacketFormat(string packet, out string normalizedPacket)
    {
        normalizedPacket = packet;

        if (string.IsNullOrWhiteSpace(packet))
        {
            _logger.LogWarning("Pacote vazio ou nulo recebido");
            return false;
        }

        if (packet.TrimStart().StartsWith("{") && packet.TrimEnd().EndsWith("}"))
        {
            try
            {
                if (packet.Contains("\"critical_event_id\"") && packet.Contains("\"cluster_size\""))
                {
                    _logger.LogInformation("Pacote JSON detectado (Módulo 5 - formato alternativo). Normalizando...");

                    var sequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var timestamp = DateTime.UtcNow.ToString("O");
                    normalizedPacket = $"MODULE5;{sequence};MODULE5;ALARM;{packet};{timestamp}";

                    _logger.LogDebug("Pacote normalizado: {Packet}",
                        normalizedPacket.Length > 200 ? normalizedPacket[..200] + "..." : normalizedPacket);

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao tentar normalizar JSON: {Packet}",
                    packet.Length > 100 ? packet[..100] + "..." : packet);
                return false;
            }
        }

        var parts = packet.Split(';');
        if (parts.Length < 6)
        {
            _logger.LogWarning("Pacote com número insuficiente de campos ({Count}). Esperado: 6. Pacote: {Packet}",
                parts.Length, packet.Length > 200 ? packet[..200] + "..." : packet);
            return false;
        }

        if (!long.TryParse(parts[1], out _))
        {
            _logger.LogWarning("Campo SEQUENCIA inválido: '{Sequence}'. Pacote: {Packet}",
                parts[1], packet.Length > 200 ? packet[..200] + "..." : packet);
            return false;
        }

        var validModules = new[] { "MODULE1", "MODULE2", "MODULE3", "MODULE4", "MODULE5", "MODULE6" };
        if (!validModules.Contains(parts[2]))
        {
            _logger.LogWarning("Módulo inválido: '{Module}'. Módulos válidos: {ValidModules}. Pacote: {Packet}",
                parts[2], string.Join(", ", validModules), packet.Length > 200 ? packet[..200] + "..." : packet);
            return false;
        }

        return true;
    }

    private async Task ProcessTcpPackets(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_tcpPacketQueue.TryDequeue(out var rawPacket))
            {
                try
                {
                    var packet = BroadcastPacket.Parse(rawPacket);
                    await _aggregationService.ProcessPacket(packet);
                    _logger.LogDebug("Pacote TCP processado: {Origin} | {Module} | {Op}",
                        packet.Origin, packet.Module, packet.OperationType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar pacote TCP: {Packet}",
                        rawPacket.Length > 100 ? rawPacket[..100] : rawPacket);
                }
            }
            else
            {
                await Task.Delay(10, cancellationToken);
            }
        }
    }

    private async Task ProcessUdpPackets(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_udpPacketQueue.TryDequeue(out var rawPacket))
            {
                try
                {
                    var packet = BroadcastPacket.Parse(rawPacket);
                    await _aggregationService.ProcessPacket(packet);
                    _logger.LogDebug("Pacote UDP processado: {Origin} | {Module} | {Op}",
                        packet.Origin, packet.Module, packet.OperationType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar pacote UDP: {Message}. Pacote: {Packet}",
                        ex.Message, rawPacket.Length > 100 ? rawPacket[..100] : rawPacket);
                }
            }
            else
            {
                await Task.Delay(10, cancellationToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BroadcastReceiverService parando");
        _cts.Cancel();
        await base.StopAsync(cancellationToken);
    }
}