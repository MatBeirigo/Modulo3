using System.Net.Sockets;
using System.Text;
using Core.Models;
using Microsoft.Extensions.Logging;

namespace Services;

public class Module6CommandService
{
    private readonly ILogger<Module6CommandService> _logger;
    private readonly DataAggregationService _aggregationService;
    private const int Module6TcpPort = 5000;
    private static readonly TimeSpan Esp32ProcessingDelay = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan Esp32ReadTimeout = TimeSpan.FromSeconds(3);

    public Module6CommandService(ILogger<Module6CommandService> logger, DataAggregationService aggregationService)
    {
        _logger = logger;
        _aggregationService = aggregationService;
    }

    public async Task<bool> SendConfigureIdAsync(int newId, string uniqueId, CancellationToken cancellationToken = default)
    {
        if (newId <= 0 || newId > 99)
        {
            _logger.LogError(
                "Tentativa de configurar ID inválido bloqueada — ID={NewId}. O ID 00 é reservado para broadcast e não pode ser atribuído a um módulo.",
                newId);
            return false;
        }

        var moduleIp = _aggregationService.GetModuleIp(uniqueId);

        if (moduleIp == null)
        {
            _logger.LogWarning(
                "IP do módulo não encontrado para UniqueID={UniqueId}. O módulo ainda não enviou broadcast UDP?",
                uniqueId);
            return false;
        }

        return await SendTcpPacketAsync(
            moduleIp,
            Module6Packet.CreateConfigPacket(newId, uniqueId),
            readResponse: false,
            onSuccess: () => _aggregationService.RegisterModuleId(newId, uniqueId),
            logContext: $"NovoID={newId}, UniqueID={uniqueId}",
            cancellationToken);
    }

    public async Task<bool> SendRelayCommandAsync(int moduleId, Module6Command command, CancellationToken cancellationToken = default)
    {
        var moduleIp = _aggregationService.GetModuleIpById(moduleId);

        if (moduleIp == null)
        {
            _logger.LogWarning(
                "IP do módulo não encontrado para ModuleID={ModuleId}.",
                moduleId);
            return false;
        }

        bool readResponse = command == Module6Command.CheckState;

        return await SendTcpPacketAsync(
            moduleIp,
            Module6Packet.CreateCommandPacket(moduleId, command),
            readResponse: readResponse,
            onSuccess: null,
            logContext: $"ModuleID={moduleId}, Cmd={command}",
            cancellationToken);
    }

    private async Task<bool> SendTcpPacketAsync(
        string ip,
        string packet,
        bool readResponse,
        Func<Task>? onSuccess,
        string logContext,
        CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(packet + "\n");

        _logger.LogInformation(
            "[TCP OUT] Conectando — IP={Ip}:{Port} | {Context}",
            ip, Module6TcpPort, logContext);

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ip, Module6TcpPort, cancellationToken);

            _logger.LogInformation(
                "[TCP OUT] Conexão estabelecida — IP={Ip}:{Port} | Local={Local}",
                ip, Module6TcpPort, client.Client.LocalEndPoint);

            var stream = client.GetStream();
            await stream.WriteAsync(bytes, cancellationToken);
            await stream.FlushAsync(cancellationToken);

            _logger.LogInformation(
                "[TCP OUT] Enviado — IP={Ip}:{Port} | Tamanho={Size} bytes | HEX={Hex} | Raw='{Packet}' | {Context}",
                ip, Module6TcpPort, bytes.Length, Convert.ToHexString(bytes), packet, logContext);

            if (readResponse)
            {
                _logger.LogInformation(
                    "[TCP OUT] Aguardando resposta — IP={Ip} | Timeout={Timeout}s",
                    ip, Esp32ReadTimeout.TotalSeconds);
                await ReadTcpResponseAsync(stream, ip, cancellationToken);
            }
            else
            {
                _logger.LogInformation(
                    "[TCP OUT] Sem leitura de resposta — aguardando delay de {Delay}ms | IP={Ip}",
                    Esp32ProcessingDelay.TotalMilliseconds, ip);
                await Task.Delay(Esp32ProcessingDelay, cancellationToken);
            }

            if (onSuccess != null)
                await onSuccess();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[TCP OUT] Erro — IP={Ip}:{Port} | {Context}",
                ip, Module6TcpPort, logContext);
            return false;
        }
    }

    private async Task ReadTcpResponseAsync(NetworkStream stream, string sourceIp, CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(Esp32ReadTimeout);

            var buffer = new byte[64];

            _logger.LogInformation(
                "[TCP IN] Aguardando bytes do ESP32 — IP={Ip} | BufferSize={Size}", sourceIp, buffer.Length);

            var bytesRead = await stream.ReadAsync(buffer, cts.Token);

            if (bytesRead == 0)
            {
                _logger.LogWarning("[TCP IN] ESP32 fechou a conexão sem enviar dados — IP={Ip}", sourceIp);
                return;
            }

            var rawBytes = buffer[..bytesRead];
            var raw = Encoding.UTF8.GetString(rawBytes).Trim();

            _logger.LogInformation(
                "[TCP IN] Recebido — IP={Ip} | Bytes={BytesRead} | HEX={Hex} | Raw='{Raw}'",
                sourceIp, bytesRead, Convert.ToHexString(rawBytes), raw);

            var parsed = Module6Packet.Parse(raw);
            parsed.SourceIp = sourceIp;

            _logger.LogInformation(
                "[TCP IN] Parseado — IP={Ip} | Prefix={Prefix} | RecipientId={Id} | Cmd={Cmd} | State={State} | UniqueId={UniqueId}",
                sourceIp, parsed.Prefix, parsed.RecipientId, parsed.Command, parsed.State, parsed.UniqueId);

            await _aggregationService.ProcessModule6Packet(parsed);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "[TCP IN] Timeout — ESP32 não respondeu em {Timeout}s — IP={Ip}",
                Esp32ReadTimeout.TotalSeconds, sourceIp);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(
                "[TCP IN] Formato inválido — Motivo={Reason} | IP={Ip}", ex.Message, sourceIp);
        }
    }
}