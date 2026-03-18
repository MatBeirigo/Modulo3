using Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

namespace Services;

public class BroadcastReceiverService : BackgroundService
{
    private readonly ILogger<BroadcastReceiverService> _logger;
    private readonly DataAggregationService _aggregationService;
    private readonly ConcurrentDictionary<string, Channel<string>> _deviceChannels = new();
    private readonly ConcurrentDictionary<string, Channel<Module6Packet>> _module6Channels = new();
    private readonly CancellationTokenSource _cts;

    private const int UdpPort = 4210;
    private const int Module6UdpPort = 4211;

    public BroadcastReceiverService(ILogger<BroadcastReceiverService> logger, DataAggregationService aggregationService)
    {
        _logger = logger;
        _aggregationService = aggregationService;
        _cts = new CancellationTokenSource();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BroadcastReceiverService iniciado");

        await Task.WhenAll(
            Task.Run(() => StartUdpListener(stoppingToken), stoppingToken),
            Task.Run(() => StartModule6UdpListener(stoppingToken), stoppingToken)
        );
    }

    private async Task StartUdpListener(CancellationToken cancellationToken)
    {
        using var udpClient = new UdpClient();
        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, UdpPort));
        udpClient.EnableBroadcast = true;

        var localEndPoint = (IPEndPoint)udpClient.Client.LocalEndPoint!;
        var networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
            .ToList();

        _logger.LogInformation("=== BroadcastReceiverService — Configurações de Escuta ===");
        _logger.LogInformation("  Protocolo    : UDP");
        _logger.LogInformation("  Endereço     : {Address} (qualquer interface)", localEndPoint.Address);
        _logger.LogInformation("  Porta geral  : {Port} (MODULE1~MODULE5)", localEndPoint.Port);
        _logger.LogInformation("  Porta Mód. 6 : {Port} (exclusiva)", Module6UdpPort);
        _logger.LogInformation("  Broadcast    : habilitado");
        _logger.LogInformation("  ReuseAddress : habilitado");
        _logger.LogInformation("  Modelo       : thread dedicada por dispositivo (Channel<T>)");
        _logger.LogInformation("  Módulos válidos: MODULE1, MODULE2, MODULE3, MODULE4, MODULE5, MODULE6");
        _logger.LogInformation("  Módulo 6 (# / !) na porta {Port} — aceito mas roteado para fila dedicada", UdpPort);
        _logger.LogInformation("  Módulo 5 JSON — normalizado automaticamente se contiver 'critical_event_id' + 'cluster_size'");

        _logger.LogInformation("=== Interfaces de rede ativas ===");
        foreach (var iface in networkInterfaces)
        {
            var ipProps = iface.GetIPProperties();
            var ipv4 = ipProps.UnicastAddresses
                .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Select(a => a.Address.ToString());

            _logger.LogInformation(
                "  [{Type}] {Name} — IPs: {Ips}",
                iface.NetworkInterfaceType, iface.Name, string.Join(", ", ipv4));
        }

        _logger.LogInformation("UDP Listener geral aguardando pacotes na porta {Port}...", UdpPort);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await udpClient.ReceiveAsync(cancellationToken);
                var rawBytes = result.Buffer;
                var packet = System.Text.Encoding.UTF8.GetString(rawBytes);
                var sourceIp = result.RemoteEndPoint.Address.ToString();
                var sourcePort = result.RemoteEndPoint.Port;

                _logger.LogInformation(
                    "[UDP:{Port} IN] De={SourceIp}:{SourcePort} | Tamanho={Size} bytes | HEX={Hex} | Raw='{Raw}'",
                    UdpPort, sourceIp, sourcePort, rawBytes.Length,
                    Convert.ToHexString(rawBytes),
                    packet.Length > 300 ? packet[..300] + "..." : packet);

                if (IsModule6Packet(packet))
                {
                    _logger.LogInformation(
                        "[UDP:{Port} IN] → Roteado para fila Módulo 6 — De={SourceIp}:{SourcePort}",
                        UdpPort, sourceIp, sourcePort);
                    HandleModule6RawPacket(packet, sourceIp);
                    continue;
                }

                if (IsValidPacketFormat(packet, out var normalizedPacket))
                {
                    var origin = normalizedPacket.Split(';')[0];
                    var channel = GetOrCreateDeviceChannel(origin);
                    await channel.Writer.WriteAsync(normalizedPacket, cancellationToken);

                    _logger.LogInformation(
                        "[UDP:{Port} IN] → Enfileirado para dispositivo '{Origin}' — De={SourceIp}:{SourcePort}",
                        UdpPort, origin, sourceIp, sourcePort);
                }
                else
                {
                    _logger.LogWarning(
                        "[UDP:{Port} IN] → REJEITADO (formato inválido) — De={SourceIp}:{SourcePort} | Raw='{Packet}'",
                        UdpPort, sourceIp, sourcePort,
                        packet.Length > 300 ? packet[..300] + "..." : packet);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("UDP Listener geral (porta {Port}) encerrado", UdpPort);
        }
    }

    private async Task StartModule6UdpListener(CancellationToken cancellationToken)
    {
        using var udpClient = new UdpClient();
        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, Module6UdpPort));
        udpClient.EnableBroadcast = true;

        _logger.LogInformation(
            "UDP Listener Módulo 6 dedicado aguardando pacotes na porta {Port}...", Module6UdpPort);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await udpClient.ReceiveAsync(cancellationToken);
                var rawBytes = result.Buffer;
                var packet = System.Text.Encoding.UTF8.GetString(rawBytes);
                var sourceIp = result.RemoteEndPoint.Address.ToString();
                var sourcePort = result.RemoteEndPoint.Port;

                _logger.LogInformation(
                    "[UDP:{Port} IN][M6] De={SourceIp}:{SourcePort} | Tamanho={Size} bytes | HEX={Hex} | Raw='{Raw}'",
                    Module6UdpPort, sourceIp, sourcePort, rawBytes.Length,
                    Convert.ToHexString(rawBytes),
                    packet.Length > 300 ? packet[..300] + "..." : packet);

                if (!IsModule6Packet(packet))
                {
                    _logger.LogWarning(
                        "[UDP:{Port} IN][M6] → REJEITADO — pacote não é do Módulo 6 — De={SourceIp}:{SourcePort} | Raw='{Raw}'",
                        Module6UdpPort, sourceIp, sourcePort,
                        packet.Length > 200 ? packet[..200] + "..." : packet);
                    continue;
                }

                HandleModule6RawPacket(packet, sourceIp);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("UDP Listener Módulo 6 (porta {Port}) encerrado", Module6UdpPort);
        }
    }

    private Channel<string> GetOrCreateDeviceChannel(string origin)
    {
        return _deviceChannels.GetOrAdd(origin, key =>
        {
            var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            _logger.LogInformation(
                "[THREAD] Novo dispositivo detectado — '{Origin}'. Thread dedicada criada.", key);

            _ = Task.Run(() => ProcessDevicePackets(key, channel.Reader, _cts.Token));

            return channel;
        });
    }

    private Channel<Module6Packet> GetOrCreateModule6Channel(string deviceKey)
    {
        return _module6Channels.GetOrAdd(deviceKey, key =>
        {
            var channel = Channel.CreateUnbounded<Module6Packet>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            _logger.LogInformation(
                "[THREAD][M6] Novo dispositivo Módulo 6 detectado — '{DeviceKey}'. Thread dedicada criada.", key);

            _ = Task.Run(() => ProcessModule6DevicePackets(key, channel.Reader, _cts.Token));

            return channel;
        });
    }

    private async Task ProcessDevicePackets(string origin, ChannelReader<string> reader, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[THREAD] Thread do dispositivo '{Origin}' iniciada.", origin);

        try
        {
            await foreach (var rawPacket in reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    var packet = BroadcastPacket.Parse(rawPacket);
                    await _aggregationService.ProcessPacket(packet);
                    _logger.LogInformation(
                        "[UDP PROC] Processado — Origin={Origin} | Module={Module} | Op={Op} | Seq={Seq} | Timestamp={Ts}",
                        packet.Origin, packet.Module, packet.OperationType, packet.Sequence, packet.Timestamp.ToString("O"));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[UDP PROC] Erro ao processar — Origin={Origin} | Raw='{Packet}'",
                        origin,
                        rawPacket.Length > 200 ? rawPacket[..200] : rawPacket);
                }
            }
        }
        catch (OperationCanceledException)
        {
            
        }

        _logger.LogInformation("[THREAD] Thread do dispositivo '{Origin}' encerrada.", origin);
    }

    private async Task ProcessModule6DevicePackets(string deviceKey, ChannelReader<Module6Packet> reader, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[THREAD][M6] Thread do dispositivo Módulo 6 '{DeviceKey}' iniciada.", deviceKey);

        try
        {
            await foreach (var packet in reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await _aggregationService.ProcessModule6Packet(packet);
                    _logger.LogInformation(
                        "[UDP PROC][M6] Processado — DeviceKey={DeviceKey} | RecipientId={Id} | Cmd={Cmd} | State={State} | IP={Ip}",
                        deviceKey, packet.RecipientId, packet.Command, packet.State, packet.SourceIp);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[UDP PROC][M6] Erro ao processar — DeviceKey={DeviceKey} | Raw='{Raw}'",
                        deviceKey, packet.RawPacket);
                }
            }
        }
        catch (OperationCanceledException)
        {
            
        }

        _logger.LogInformation("[THREAD][M6] Thread do dispositivo Módulo 6 '{DeviceKey}' encerrada.", deviceKey);
    }

    private static bool IsModule6Packet(string packet)
        => !string.IsNullOrEmpty(packet) && (packet.StartsWith('#') || packet.StartsWith('!'));

    private void HandleModule6RawPacket(string raw, string sourceIp)
    {
        try
        {
            var parsed = Module6Packet.Parse(raw);
            parsed.SourceIp = sourceIp;

            var deviceKey = $"MODULE6-{sourceIp}";
            var channel = GetOrCreateModule6Channel(deviceKey);
            channel.Writer.TryWrite(parsed);

            _logger.LogInformation(
                "[UDP IN][M6] Pacote Módulo 6 parseado — Raw='{Raw}' | IP={Ip} | Prefix={Prefix} | RecipientId={Id} | Cmd={Cmd} | State={State} | UniqueId={UniqueId}",
                raw, sourceIp, parsed.Prefix, parsed.RecipientId, parsed.Command, parsed.State, parsed.UniqueId);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(
                "[UDP IN][M6] Pacote Módulo 6 com formato inválido — Motivo={Reason} | Raw='{Raw}' | IP={Ip}",
                ex.Message, raw, sourceIp);
        }
    }

    private bool IsValidPacketFormat(string packet, out string normalizedPacket)
    {
        normalizedPacket = packet;

        if (string.IsNullOrWhiteSpace(packet))
        {
            _logger.LogWarning("[UDP VAL] Pacote vazio ou nulo recebido");
            return false;
        }

        if (packet.TrimStart().StartsWith("{") && packet.TrimEnd().EndsWith("}"))
        {
            try
            {
                var jsonNormalized = packet.Replace('\'', '"');

                if (jsonNormalized.Contains("\"critical_event_id\"") && jsonNormalized.Contains("\"cluster_size\""))
                {
                    _logger.LogInformation("[UDP VAL] Pacote JSON Módulo 5 detectado — normalizando...");
                    var sequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var timestamp = DateTime.UtcNow.ToString("O");
                    normalizedPacket = $"MODULE5;{sequence};MODULE5;ALARM;{jsonNormalized};{timestamp}";
                    _logger.LogInformation(
                        "[UDP VAL] JSON Módulo 5 normalizado — Seq={Seq} | Normalizado='{Normalized}'",
                        sequence,
                        normalizedPacket.Length > 300 ? normalizedPacket[..300] + "..." : normalizedPacket);
                    return true;
                }

                if (jsonNormalized.Contains("\"Ia\"") && jsonNormalized.Contains("\"idDispositivo\""))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(jsonNormalized);
                    var root = doc.RootElement;

                    var deviceId = $"IED-{root.GetProperty("idDispositivo").GetInt32():D3}";
                    var sequence = root.GetProperty("numPacote").GetInt64();
                    var ia = root.GetProperty("Ia").GetDouble();
                    var ib = root.TryGetProperty("Ib", out var ibEl) ? ibEl.GetDouble() : ia;
                    var ic = root.TryGetProperty("Ic", out var icEl) ? icEl.GetDouble() : ia;
                    var current = (ia + ib + ic) / 3.0;
                    var timestamp = DateTime.UtcNow.ToString("O");

                    var data = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Voltage = 220.0,
                        Current = current,
                        Frequency = 60.0,
                        PowerFactor = 1.0,
                        Status = "NORMAL",
                        Ia = ia,
                        Ib = ib,
                        Ic = ic
                    });

                    normalizedPacket = $"{deviceId};{sequence};MODULE1;MEASUREMENT;{data};{timestamp}";

                    _logger.LogInformation(
                        "[UDP VAL] Pacote dispositivo real normalizado — Device={Device} | Seq={Seq} | Ia={Ia} | Ib={Ib} | Ic={Ic}",
                        deviceId, sequence, ia, ib, ic);
                    return true;
                }

                _logger.LogWarning(
                    "[UDP VAL] JSON recebido mas formato não reconhecido — REJEITADO | Raw='{Raw}'",
                    packet.Length > 200 ? packet[..200] + "..." : packet);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[UDP VAL] Erro ao normalizar JSON — Raw='{Packet}'",
                    packet.Length > 100 ? packet[..100] + "..." : packet);
                return false;
            }
        }

        var parts = packet.Split(';');
        if (parts.Length < 6)
        {
            _logger.LogWarning(
                "[UDP VAL] Campos insuficientes — Esperado=6 | Recebido={Count} | Raw='{Packet}'",
                parts.Length, packet.Length > 200 ? packet[..200] + "..." : packet);
            return false;
        }

        if (!long.TryParse(parts[1], out _))
        {
            _logger.LogWarning(
                "[UDP VAL] Sequência inválida — Campo[1]='{Sequence}' | Raw='{Packet}'",
                parts[1], packet.Length > 200 ? packet[..200] + "..." : packet);
            return false;
        }

        var validModules = new[] { "MODULE1", "MODULE2", "MODULE3", "MODULE4", "MODULE5", "MODULE6" };
        if (!validModules.Contains(parts[2]))
        {
            _logger.LogWarning(
                "[UDP VAL] Módulo inválido — Campo[2]='{Module}' | Válidos=[{Valid}] | Raw='{Packet}'",
                parts[2], string.Join(", ", validModules),
                packet.Length > 200 ? packet[..200] + "..." : packet);
            return false;
        }

        return true;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BroadcastReceiverService parando — encerrando todos os canais...");
        _cts.Cancel();

        foreach (var channel in _deviceChannels.Values)
            channel.Writer.TryComplete();

        foreach (var channel in _module6Channels.Values)
            channel.Writer.TryComplete();

        await base.StopAsync(cancellationToken);
    }
}