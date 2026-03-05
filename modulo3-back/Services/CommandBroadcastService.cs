using Core.Models;
using Microsoft.Extensions.Logging;
using Services;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Services;

public class CommandBroadcastService
{
    private readonly ILogger<CommandBroadcastService> _logger;
    private readonly DataAggregationService _aggregationService;
    private long _sequenceCounter;
    private const string BroadcastAddress = "255.255.255.255";
    private const int BroadcastPort = 5010;

    public CommandBroadcastService(ILogger<CommandBroadcastService> logger, DataAggregationService aggregationService)
    {
        _logger = logger;
        _aggregationService = aggregationService;
        _sequenceCounter = 0;
    }

    public async Task<string> SendCommand(string deviceId, string commandType, string targetState)
    {
        var sequence = Interlocked.Increment(ref _sequenceCounter);
        var data = System.Text.Json.JsonSerializer.Serialize(new
        {
            DeviceId = deviceId,
            CommandType = commandType,
            TargetState = targetState
        });

        var packet = BroadcastPacket.CreatePacket("MODULE3", sequence, "MODULE3", commandType, data);

        using var udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;
        var bytes = Encoding.UTF8.GetBytes(packet);

        await udpClient.SendAsync(bytes, new IPEndPoint(IPAddress.Parse(BroadcastAddress), BroadcastPort));

        var command = new SwitchCommand
        {
            DeviceId = deviceId,
            CommandType = commandType,
            TargetState = targetState,
            IsPending = true
        };

        _aggregationService.RegisterCommand(command);

        _logger.LogInformation("Comando enviado: Device={Device}, Type={Type}, Target={Target}", deviceId, commandType, targetState);

        return $"CMD-{sequence}";
    }
}