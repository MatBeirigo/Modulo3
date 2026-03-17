using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Test;

public class ModuleSimulator
{
    private readonly string _moduleId;
    private readonly string _host;
    private readonly int _port;
    private readonly bool _useTcp;
    private long _sequence = 0;

    public ModuleSimulator(string moduleId, string host = "127.0.0.1", int port = 4210, bool useTcp = false)
    {
        _moduleId = moduleId;
        _host = host;
        _port = port;
        _useTcp = useTcp;
    }

    public async Task SendMeasurement(string deviceId, double voltage, double current, double frequency, double powerFactor)
    {
        var data = JsonSerializer.Serialize(new
        {
            Voltage = voltage,
            Current = current,
            Frequency = frequency,
            PowerFactor = powerFactor,
            Status = "NORMAL"
        });

        await SendPacket(deviceId, "MODULE1", "MEASUREMENT", data);
    }

    /// <summary>
    /// Envia evento de proteção do MODULE2 com metadados numéricos reais.
    /// </summary>
    public async Task SendProtectionEvent(
        string deviceId,
        string eventType,
        string severity,
        bool isStart,
        string phase = "A",
        string function = "50",
        double pickupA = 5.0,
        double measuredA = 0.0,
        string? duration = null,
        string? resolvedBy = null)
    {
        object metadata;

        if (isStart)
        {
            // EVENT_START não tem Duration nem ResolvedBy
            metadata = new
            {
                Function = function,
                Phase = phase,
                PickupA = pickupA,
                MeasuredA = measuredA > 0 ? measuredA : pickupA * (1.1 + Random.Shared.NextDouble() * 0.5),
                ThresholdExceeded = $"{(int)((measuredA / pickupA) * 100)}%"
            };
        }
        else
        {
            // EVENT_END tem Duration e ResolvedBy
            metadata = new
            {
                Function = function,
                Phase = phase,
                PickupA = pickupA,
                MeasuredA = measuredA > 0 ? measuredA : pickupA * (0.3 + Random.Shared.NextDouble() * 0.3),
                ThresholdExceeded = $"{(int)((measuredA / pickupA) * 100)}%",
                Duration = duration ?? $"{Random.Shared.Next(300, 700)}ms",
                ResolvedBy = resolvedBy ?? "AUTO"
            };
        }

        var data = JsonSerializer.Serialize(new
        {
            EventType = eventType,
            Severity = severity,
            Metadata = metadata
        });

        var operationType = isStart ? "EVENT_START" : "EVENT_END";
        await SendPacket(deviceId, "MODULE2", operationType, data);
    }

    public async Task SendEventReport(string eventType, int totalCount, Dictionary<string, int> countByDevice, string windowDuration = "30s")
    {
        var data = JsonSerializer.Serialize(new
        {
            EventType = eventType,
            TotalCount = totalCount,
            CountByDevice = countByDevice,
            WindowDuration = windowDuration
        });

        await SendPacket(_moduleId, "MODULE4", "REPORT", data);
    }

    public async Task SendAggregatedAlarm(string eventType, int clusterSize, double latitude, double longitude)
    {
        var data = JsonSerializer.Serialize(new
        {
            critical_event_id = Guid.NewGuid().ToString(),
            critical_event_type = eventType,
            local = new[] { latitude, longitude },
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            cluster_size = clusterSize
        }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await SendPacket(_moduleId, "MODULE5", "ALARM", data);
    }

    private async Task SendPacket(string origin, string module, string operationType, string data)
    {
        var sequence = Interlocked.Increment(ref _sequence);
        var timestamp = DateTime.UtcNow;
        var packet = $"{origin};{sequence};{module};{operationType};{data};{timestamp:O}";

        try
        {
            if (_useTcp)
                await SendViaTcp(packet);
            else
                await SendViaUdp(packet);

            Console.WriteLine($"[{module}] Pacote enviado: {origin} | Seq: {sequence} | Op: {operationType}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERRO] Falha ao enviar pacote: {ex.Message}");
        }
    }

    private async Task SendViaTcp(string packet)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_host, 5555);
        var stream = client.GetStream();
        var bytes = Encoding.UTF8.GetBytes(packet);
        await stream.WriteAsync(bytes);
    }

    private async Task SendViaUdp(string packet)
    {
        using var client = new UdpClient();
        client.EnableBroadcast = true;
        var bytes = Encoding.UTF8.GetBytes(packet);

        IPAddress ipAddress;
        if (IPAddress.TryParse(_host, out var parsedIp))
            ipAddress = parsedIp;
        else
        {
            var hostEntry = await Dns.GetHostEntryAsync(_host);
            ipAddress = hostEntry.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                ?? hostEntry.AddressList.First();
        }

        var endpoint = new IPEndPoint(ipAddress, _port);
        await client.SendAsync(bytes, endpoint);
    }
}