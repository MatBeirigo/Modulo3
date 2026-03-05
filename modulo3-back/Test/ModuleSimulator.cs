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

    public ModuleSimulator(string moduleId, string host = "localhost", int port = 5002, bool useTcp = false)
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

    public async Task SendProtectionEvent(string deviceId, string eventType, string severity, bool isStart = true)
    {
        var data = JsonSerializer.Serialize(new
        {
            EventType = eventType,
            Severity = severity,
            Metadata = new Dictionary<string, string>
            {
                { "Location", "Bay-A" },
                { "Phase", "ABC" }
            }
        });

        var operationType = isStart ? "EVENT_START" : "EVENT_END";
        await SendPacket(deviceId, "MODULE2", operationType, data);
    }

    public async Task SendEventReport(string eventType, int totalCount, Dictionary<string, int> countByDevice)
    {
        var data = JsonSerializer.Serialize(new
        {
            EventType = eventType,
            TotalCount = totalCount,
            CountByDevice = countByDevice,
            WindowDuration = "00:01:00"
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

    public async Task SendStateUpdate(string deviceId, string state)
    {
        var data = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            { "DeviceId", deviceId },
            { "State", state }
        });

        await SendPacket(deviceId, "MODULE6", "STATE_UPDATE", data);
    }

    private async Task SendPacket(string origin, string module, string operationType, string data)
    {
            var sequence = Interlocked.Increment(ref _sequence);
            var timestamp = DateTime.UtcNow;
        var packet = $"{origin};{sequence};{module};{operationType};{data};{timestamp:O}";

        try
        {
            if (_useTcp)
            {
                await SendViaTcp(packet);
            }
            else
            {
                await SendViaUdp(packet);
            }

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
        var bytes = Encoding.UTF8.GetBytes(packet);

        IPAddress ipAddress;
        if (IPAddress.TryParse(_host, out var parsedIp))
        {
            ipAddress = parsedIp;
        }
        else
        {
            var hostEntry = await Dns.GetHostEntryAsync(_host);
            ipAddress = hostEntry.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                        ?? hostEntry.AddressList.First();
        }

        var endpoint = new IPEndPoint(ipAddress, _port);
        await client.SendAsync(bytes, endpoint);
    }
}