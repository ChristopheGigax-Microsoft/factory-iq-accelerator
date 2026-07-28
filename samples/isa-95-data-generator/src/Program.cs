using Isa95DataGenerator.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        var connectionString = Environment.GetEnvironmentVariable("IoTHubDeviceConnectionString")
            ?? throw new InvalidOperationException("IoTHubDeviceConnectionString is not configured.");

        services.AddSingleton<IIoTHubService>(sp =>
            new IoTHubService(connectionString, sp.GetRequiredService<ILogger<IoTHubService>>()));

        services.AddSingleton<IScenarioController, ScenarioController>();
        services.AddSingleton<ITelemetryGenerator, TelemetryGenerator>();
        services.AddSingleton<IMachineStateGenerator, MachineStateGenerator>();
        services.AddSingleton<IWorkOrderOrchestrator, WorkOrderOrchestrator>();
    })
    .Build();

host.Run();
