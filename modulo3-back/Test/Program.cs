using Test;

Console.WriteLine("=== Simulador de Módulos - Control API ===\n");

// Uso: dotnet run -- module1
//      dotnet run -- module2
//      dotnet run -- module4
//      dotnet run -- module5
//      dotnet run -- module6
//      dotnet run           (roda todos)
var moduleArg = args.FirstOrDefault()?.ToLowerInvariant() ?? "all";

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

const string serverHost = "192.168.0.255";
const int udpPort = 4210;

var simulator = new ModuleSimulator("TEST-MODULE", serverHost, udpPort, useTcp: false);
var module6Simulator = new Module6Simulator();

// Faixa de detecção de curto do Módulo 2
const double shortCircuitLow = 10.0;
const double shortCircuitHigh = 12.0;

var devices = new[]
{
    new { Id = "MU-001", PickupA = shortCircuitLow, Phase = "A" },
    new { Id = "MU-002", PickupA = shortCircuitLow, Phase = "C" },
    new { Id = "MU-003", PickupA = shortCircuitLow, Phase = "B" },
};

Console.WriteLine($"Módulo   : {moduleArg}");
Console.WriteLine($"Servidor : {serverHost}:{udpPort}");
Console.WriteLine($"Devices  : {string.Join(", ", devices.Select(d => d.Id))}");
Console.WriteLine($"Faixa curto: {shortCircuitLow}A – {shortCircuitHigh}A");
Console.WriteLine("\nPressione CTRL+C para parar o simulador...\n");

var tasks = new List<Task>();

// ── MODULE1 ───────────────────────────────────────────────────────────────────
if (moduleArg is "all" or "module1")
{
    foreach (var device in devices)
    {
        var d = device;
        tasks.Add(Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                await simulator.SendMeasurement(
                    d.Id,
                    voltage: 220 + Random.Shared.NextDouble() * 10,
                    current: 15 + Random.Shared.NextDouble() * 5,
                    frequency: 60 + Random.Shared.NextDouble() * 0.5,
                    powerFactor: 0.92 + Random.Shared.NextDouble() * 0.06
                );
                await Task.Delay(1000, cts.Token);
            }
        }, cts.Token));
    }
    Console.WriteLine("[MODULE1] Iniciado — medições contínuas");
}

// ── MODULE2 ───────────────────────────────────────────────────────────────────
if (moduleArg is "all" or "module2")
{
    foreach (var device in devices)
    {
        var d = device;
        tasks.Add(Task.Run(async () =>
        {
            await Task.Delay(Random.Shared.Next(2000, 6000), cts.Token);

            while (!cts.Token.IsCancellationRequested)
            {
                // Alterna entre 3 cenários: abaixo, dentro e acima da faixa
                var cenario = Random.Shared.Next(0, 3);

                double measuredStart = cenario switch
                {
                    // Abaixo da faixa — corrente normal (6A–9.9A)
                    0 => shortCircuitLow * (0.6 + Random.Shared.NextDouble() * 0.39),
                    // Dentro da faixa — curto detectado (10A–12A)
                    1 => shortCircuitLow + Random.Shared.NextDouble() * (shortCircuitHigh - shortCircuitLow),
                    // Acima da faixa — curto severo (12.1A–15A)
                    _ => shortCircuitHigh + 0.1 + Random.Shared.NextDouble() * 2.9
                };

                var label = cenario switch
                {
                    0 => "ABAIXO DA FAIXA",
                    1 => "DENTRO DA FAIXA",
                    _ => "ACIMA DA FAIXA"
                };

                await simulator.SendProtectionEvent(
                    deviceId: d.Id,
                    eventType: "OVERCURRENT",
                    severity: "CRITICAL",
                    isStart: true,
                    phase: d.Phase,
                    function: "50",
                    pickupA: d.PickupA,
                    measuredA: measuredStart
                );
                Console.WriteLine(
                    $"*** [MODULE2] EVENT_START — {d.Id} | Fase={d.Phase} | {label} | Medido={measuredStart:F3}A (pickup={d.PickupA}A) ***");

                var durationMs = Random.Shared.Next(300, 700);
                await Task.Delay(durationMs, cts.Token);

                // Corrente de retorno sempre abaixo do limiar inferior (curto resolvido)
                var measuredEnd = shortCircuitLow * (0.3 + Random.Shared.NextDouble() * 0.25);

                await simulator.SendProtectionEvent(
                    deviceId: d.Id,
                    eventType: "OVERCURRENT",
                    severity: "CRITICAL",
                    isStart: false,
                    phase: d.Phase,
                    function: "50",
                    pickupA: d.PickupA,
                    measuredA: measuredEnd,
                    duration: $"{durationMs}ms",
                    resolvedBy: "AUTO"
                );
                Console.WriteLine(
                    $"*** [MODULE2] EVENT_END   — {d.Id} | Fase={d.Phase} | Retorno={measuredEnd:F3}A | Duração={durationMs}ms ***");

                await Task.Delay(Random.Shared.Next(1500, 4000), cts.Token);
            }
        }, cts.Token));
    }
    Console.WriteLine($"[MODULE2] Iniciado — cenários: abaixo(<{shortCircuitLow}A), dentro({shortCircuitLow}–{shortCircuitHigh}A), acima(>{shortCircuitHigh}A)");
}

// ── MODULE4 ───────────────────────────────────────────────────────────────────
if (moduleArg is "all" or "module4")
{
    tasks.Add(Task.Run(async () =>
    {
        await Task.Delay(10000, cts.Token);
        while (!cts.Token.IsCancellationRequested)
        {
            await simulator.SendEventReport(
                eventType: "OVERCURRENT",
                totalCount: devices.Length * Random.Shared.Next(3, 10),
                countByDevice: devices.ToDictionary(d => d.Id, _ => Random.Shared.Next(1, 8)),
                windowDuration: "30s"
            );
            Console.WriteLine("--- [MODULE4] Relatório de eventos enviado ---");
            await Task.Delay(30000, cts.Token);
        }
    }, cts.Token));
    Console.WriteLine("[MODULE4] Iniciado — relatórios periódicos a cada 30s");
}

// ── MODULE5 ───────────────────────────────────────────────────────────────────
if (moduleArg is "all" or "module5")
{
    tasks.Add(Task.Run(async () =>
    {
        await Task.Delay(8000, cts.Token);
        while (!cts.Token.IsCancellationRequested)
        {
            await simulator.SendAggregatedAlarm(
                eventType: "VOLTAGE_DROP",
                clusterSize: Random.Shared.Next(10, 60),
                latitude: -18.9123,
                longitude: -48.2755
            );
            Console.WriteLine("!!! [MODULE5] Alarme agregado enviado !!!");
            await Task.Delay(45000, cts.Token);
        }
    }, cts.Token));
    Console.WriteLine("[MODULE5] Iniciado — alarmes a cada 45s");
}

// ── MODULE6 ───────────────────────────────────────────────────────────────────
if (moduleArg is "all" or "module6")
{
    tasks.Add(module6Simulator.Start(cts.Token));
    Console.WriteLine("[MODULE6] Iniciado — TCP listener + broadcast UDP");
}

Console.WriteLine();

if (tasks.Count == 0)
{
    Console.WriteLine($"Módulo '{moduleArg}' desconhecido. Opções: module1, module2, module4, module5, module6, all");
    return;
}

try
{
    await Task.WhenAll(tasks);
}
catch (OperationCanceledException)
{
    Console.WriteLine("\n=== Simuladores encerrados ===");
}