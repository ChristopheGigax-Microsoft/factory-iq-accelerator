using Microsoft.Extensions.Logging;
using OpcUaDataGenerator.Server;
using OpcUaDataGenerator.Services;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSimpleConsole(o =>
    {
        o.SingleLine = true;
        o.TimestampFormat = "HH:mm:ss ";
    });
    builder.SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger("Program");

var scenario = new ScenarioController();
var telemetryGenerator = new TelemetryGenerator(scenario, loggerFactory.CreateLogger<TelemetryGenerator>());
var stateGenerator = new MachineStateGenerator(scenario, loggerFactory.CreateLogger<MachineStateGenerator>());

logger.LogInformation("Starting FactoryIQ OPC UA Data Generator — scenario: {scenario}", scenario.Current);

string configPath = Path.Combine(AppContext.BaseDirectory, "Server", "FactoryOpcUaServer.Config.xml");
var (server, application) = await FactoryOpcUaServer.StartAsync(configPath);

logger.LogInformation("OPC UA server listening at opc.tcp://localhost:4855/FactoryIQ/OpcUaDataGenerator");
logger.LogInformation("Address space root: Objects/FactoryIQ/site-lyon-edge/area-lyon-motor-line/line-lyon-motor-01/...");
logger.LogInformation("Press Ctrl+C to stop.");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// Fast tick every 10 s — equipment telemetry + state/alarm transitions.
using var fastTimer = new PeriodicTimer(TimeSpan.FromSeconds(10));

try
{
    while (await fastTimer.WaitForNextTickAsync(cts.Token))
    {
        var readings = telemetryGenerator.GenerateSignals();
        server.NodeManager.ApplyTelemetry(readings);

        var changes = stateGenerator.GenerateStateChanges();
        if (changes.Count > 0)
        {
            server.NodeManager.ApplyStateChanges(changes, stateGenerator.ActiveAlarms);
        }

        logger.LogDebug("Tick applied: {signals} signals, {states} state changes", readings.Count, changes.Count);
    }
}
catch (OperationCanceledException)
{
    // graceful shutdown
}
finally
{
    logger.LogInformation("Stopping OPC UA server...");
    server.Dispose();
}
