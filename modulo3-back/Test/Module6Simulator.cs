using System.Net;
using System.Net.Sockets;
using System.Text;
using Core.Models;

namespace Test;

public class Module6Simulator
{
    // Estados dos relés: chave = ID numérico do módulo, valor = "01" (fechado) ou "00" (aberto)
    private readonly Dictionary<int, string> _relayStates = new();
    private readonly Dictionary<int, string> _uniqueIds = new();

    private const int TcpPort = 5000;
    private const int UdpPort = 4210;
    private const string ServerHost = "127.0.0.1";

    public async Task Start(CancellationToken cancellationToken)
    {
        // Simula 3 módulos: IDs 5, 10, 15
        _relayStates[5] = "01"; // fechado
        _relayStates[10] = "00"; // aberto
        _relayStates[15] = "01"; // fechado

        // UniqueIDs simulados (12 chars)
        _uniqueIds[5] = "F499540B65F4";
        _uniqueIds[10] = "A1B2C3D4E5F6";
        _uniqueIds[15] = "1A2B3C4D5E6F";

        Console.WriteLine("=== Módulo 6 Simulator iniciado ===");
        Console.WriteLine($"  TCP escuta na porta {TcpPort}");
        Console.WriteLine($"  UDP envia para {ServerHost}:{UdpPort}\n");

        var tcpTask = StartTcpServer(cancellationToken);
        var broadcastTask = BroadcastUnconfigured(cancellationToken);
        var stateTask = BroadcastStates(cancellationToken);

        await Task.WhenAll(tcpTask, broadcastTask, stateTask);
    }

    private async Task StartTcpServer(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Any, TcpPort);
        listener.Start();
        Console.WriteLine($"[MODULE6] TCP Listener iniciado na porta {TcpPort}");

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
            Console.WriteLine("[MODULE6] TCP Listener encerrado");
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
            var buffer = new byte[256];

            try
            {
                var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0) return;

                var raw = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                Console.WriteLine($"[MODULE6] TCP recebido: {raw}");

                var packet = Module6Packet.Parse(raw);
                await HandleCommand(packet, stream, cancellationToken);
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"[MODULE6] Pacote inválido: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MODULE6] Erro TCP: {ex.Message}");
            }
        }
    }

    private async Task HandleCommand(Module6Packet packet, NetworkStream stream, CancellationToken cancellationToken)
    {
        switch (packet.Command)
        {
            case Module6Command.ConfigureId:
                // #00;9;05;F499540B65F4 — configura ID para o módulo com esse UniqueID
                if (packet.UniqueId != null && _uniqueIds.ContainsValue(packet.UniqueId))
                {
                    var oldId = _uniqueIds.First(kv => kv.Value == packet.UniqueId).Key;
                    var newId = packet.RecipientId;

                    if (!_relayStates.ContainsKey(newId))
                    {
                        _relayStates[newId] = _relayStates[oldId];
                        _relayStates.Remove(oldId);
                        _uniqueIds[newId] = packet.UniqueId;
                        _uniqueIds.Remove(oldId);
                    }

                    Console.WriteLine($"[MODULE6] ID configurado: {oldId} → {newId} (UniqueID={packet.UniqueId})");
                }
                break;

            case Module6Command.CloseRelay:
                // #10;1;00 — fecha o relé do módulo 10
                if (_relayStates.ContainsKey(packet.RecipientId))
                {
                    _relayStates[packet.RecipientId] = "01";
                    Console.WriteLine($"[MODULE6] Relé FECHADO: ID={packet.RecipientId:D2}");
                }
                break;

            case Module6Command.OpenRelay:
                // #10;2;00 — abre o relé do módulo 10
                if (_relayStates.ContainsKey(packet.RecipientId))
                {
                    _relayStates[packet.RecipientId] = "00";
                    Console.WriteLine($"[MODULE6] Relé ABERTO: ID={packet.RecipientId:D2}");
                }
                break;

            case Module6Command.CheckState:
                // #10;3;99 — módulo de visualização (99) pergunta o estado do relé 10
                // Resposta: !99;3;01
                if (_relayStates.TryGetValue(packet.RecipientId, out var state))
                {
                    var response = Module6Packet.CreateStateResponse(int.Parse(packet.State), state);
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(responseBytes, cancellationToken);
                    Console.WriteLine($"[MODULE6] Estado enviado: {response}");
                }
                break;

            default:
                Console.WriteLine($"[MODULE6] Comando desconhecido: {packet.Command}");
                break;
        }
    }

    /// <summary>
    /// Simula módulos sem configuração enviando broadcast UDP periodicamente.
    /// Formato: #00;0;F499540B65F4
    /// </summary>
    private async Task BroadcastUnconfigured(CancellationToken cancellationToken)
    {
        using var udpClient = new UdpClient();
        var endpoint = new IPEndPoint(IPAddress.Parse(ServerHost), UdpPort);

        // Simula um módulo ainda não configurado com UniqueID fixo
        const string unconfiguredUniqueId = "DEADBEEF0001";

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(15000, cancellationToken);

            var packet = $"#00;0;00;{unconfiguredUniqueId}";
            var bytes = Encoding.UTF8.GetBytes(packet);
            await udpClient.SendAsync(bytes, endpoint);

            Console.WriteLine($"[MODULE6] Broadcast sem config: {packet}");
        }
    }

    /// <summary>
    /// Envia estado de relés configurados periodicamente via UDP.
    /// </summary>
    private async Task BroadcastStates(CancellationToken cancellationToken)
    {
        using var udpClient = new UdpClient();
        var endpoint = new IPEndPoint(IPAddress.Parse(ServerHost), UdpPort);

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(10000, cancellationToken);

            foreach (var (id, state) in _relayStates)
            {
                // Envia resposta de estado no formato do módulo de visualização
                var packet = Module6Packet.CreateStateResponse(id, state);
                var bytes = Encoding.UTF8.GetBytes(packet);
                await udpClient.SendAsync(bytes, endpoint);

                Console.WriteLine($"[MODULE6] Estado broadcast: {packet} (relé {(state == "01" ? "FECHADO" : "ABERTO")})");
            }
        }
    }
}