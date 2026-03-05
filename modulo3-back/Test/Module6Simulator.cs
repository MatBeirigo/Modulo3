using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Test;

public class Module6Simulator
{
    private readonly Dictionary<string, string> _switchStates = new();
    private const int Port = 5006;
    private long _sequence = 0;

    public async Task Start(CancellationToken cancellationToken)
    {
        _switchStates["SWITCH-001"] = "CLOSED";
        _switchStates["SWITCH-002"] = "OPEN";
        _switchStates["BREAKER-13A"] = "CLOSED";

        Console.WriteLine("=== Módulo 6 Simulator iniciado na porta 5006 ===\n");

        var tcpTask = StartTcpServer(cancellationToken);
        var broadcastTask = BroadcastStates(cancellationToken);

        await Task.WhenAll(tcpTask, broadcastTask);
    }

    private async Task StartTcpServer(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();

        Console.WriteLine("[MODULE6] TCP Listener iniciado na porta 5006");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken);
                _ = Task.Run(() => HandleClient(client, cancellationToken), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[MODULE6] TCP Listener encerrado");
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClient(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            var stream = client.GetStream();
            var buffer = new byte[8192];

            try
            {
                var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0) return;

                var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"[MODULE6] Requisição recebida");

                if (request.Contains("STATE_REQUEST"))
                {
                    foreach (var kvp in _switchStates)
                    {
                        var response = CreateStatePacket(kvp.Key, kvp.Value);
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        await stream.WriteAsync(responseBytes, cancellationToken);
                        await Task.Delay(10, cancellationToken);
                    }
                }
                else if (request.Contains("COMMAND_OPEN") || request.Contains("COMMAND_CLOSE"))
                {
                    var parts = request.Split(';');
                    if (parts.Length >= 5)
                    {
                        var deviceId = parts[0];
                        var newState = request.Contains("COMMAND_OPEN") ? "OPEN" : "CLOSED";

                        if (_switchStates.ContainsKey(deviceId))
                        {
                            _switchStates[deviceId] = newState;
                            Console.WriteLine($"[MODULE6] Estado alterado: {deviceId} = {newState}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MODULE6] Erro: {ex.Message}");
            }
        }
    }

    private async Task BroadcastStates(CancellationToken cancellationToken)
    {
        using var udpClient = new UdpClient();
        var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5002);

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(10000, cancellationToken);

            foreach (var kvp in _switchStates)
            {
                var packet = CreateStatePacket(kvp.Key, kvp.Value);
                var bytes = Encoding.UTF8.GetBytes(packet);
                await udpClient.SendAsync(bytes, endpoint);

                Console.WriteLine($"[MODULE6] Estado enviado: {kvp.Key} = {kvp.Value}");
            }
        }
    }

    private string CreateStatePacket(string deviceId, string state)
    {
        var sequence = Interlocked.Increment(ref _sequence);
        var timestamp = DateTime.UtcNow;
        var data = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            { "DeviceId", deviceId },
            { "State", state }
        });

        return $"{deviceId};{sequence};MODULE6;STATE_UPDATE;{data};{timestamp:O}";
    }
}