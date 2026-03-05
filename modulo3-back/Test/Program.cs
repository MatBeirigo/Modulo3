using Test;

Console.WriteLine("=== Simulador de Módulos - Control API ===\n");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var simulator = new ModuleSimulator("TEST-MODULE", "localhost", 5002, useTcp: false);
var module6Simulator = new Module6Simulator();

Console.WriteLine("Iniciando envio de dados de teste...\n");

var tasks = new List<Task>();

tasks.Add(module6Simulator.Start(cts.Token));

tasks.Add(Task.Run(async () =>
{
    for (int i = 0; i < 100; i++)
    {
        await simulator.SendMeasurement(
            "IED-001",
            220 + Random.Shared.NextDouble() * 10,
            15 + Random.Shared.NextDouble() * 5,
            60 + Random.Shared.NextDouble() * 0.5,
            0.95 + Random.Shared.NextDouble() * 0.05
        );
        await Task.Delay(1000);
    }
}, cts.Token));

tasks.Add(Task.Run(async () =>
{
    for (int i = 0; i < 100; i++)
    {
        await simulator.SendMeasurement(
            "IED-002",
            225 + Random.Shared.NextDouble() * 8,
            18 + Random.Shared.NextDouble() * 4,
            60 + Random.Shared.NextDouble() * 0.3,
            0.92 + Random.Shared.NextDouble() * 0.06
        );
        await Task.Delay(1500);
    }
}, cts.Token));

tasks.Add(Task.Run(async () =>
{
    await Task.Delay(5000);
    await simulator.SendProtectionEvent("IED-001", "OVERCURRENT", "HIGH", isStart: true);
    Console.WriteLine("*** Evento de proteção INICIADO em IED-001 ***");

    await Task.Delay(10000);
    await simulator.SendProtectionEvent("IED-001", "OVERCURRENT", "HIGH", isStart: false);
    Console.WriteLine("*** Evento de proteção FINALIZADO em IED-001 ***");
}, cts.Token));

tasks.Add(Task.Run(async () =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        await Task.Delay(30000, cts.Token);
        await simulator.SendEventReport("OVERCURRENT", 15, new Dictionary<string, int>
        {
            { "IED-001", 8 },
            { "IED-002", 7 }
        });
        Console.WriteLine("--- Relatório de eventos enviado ---");
    }
}, cts.Token));

tasks.Add(Task.Run(async () =>
{
    await Task.Delay(8000, cts.Token);
    await simulator.SendAggregatedAlarm(
        eventType: "VOLTAGE_DROP",
        clusterSize: 52,
        latitude: -18.9123,
        longitude: -48.2755
    );
    Console.WriteLine("!!! Alarme agregado enviado !!!");
}, cts.Token));

Console.WriteLine("\nPressione CTRL+C para parar o simulador...\n");

try
{
    await Task.WhenAll(tasks);
}
catch (OperationCanceledException)
{
    Console.WriteLine("\n=== Simuladores encerrados ===");
}