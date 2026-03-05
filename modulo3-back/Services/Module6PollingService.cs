using Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services;
using System.Net.Sockets;
using System.Text;

namespace Services;

public class Module6PollingService : BackgroundService
{
    private readonly ILogger<Module6PollingService> _logger;
    private readonly DataAggregationService _aggregationService;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private const string Module6Host = "localhost";
    private const int Module6Port = 5006;

    public Module6PollingService(ILogger<Module6PollingService> logger, DataAggregationService aggregationService)
    {
        _logger = logger;
        _aggregationService = aggregationService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Module6PollingService iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollModule6States(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer polling do Módulo 6");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task PollModule6States(CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(Module6Host, Module6Port, cancellationToken);

            var stream = client.GetStream();
            var request = BroadcastPacket.CreatePacket("MODULE3", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), "MODULE3", "STATE_REQUEST", "{}");
            var requestBytes = Encoding.UTF8.GetBytes(request);

            await stream.WriteAsync(requestBytes, cancellationToken);

            var buffer = new byte[8192];
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken);

            if (bytesRead > 0)
            {
                var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var packet = BroadcastPacket.Parse(response);
                await _aggregationService.ProcessPacket(packet);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao conectar com Módulo 6");
        }
    }
}