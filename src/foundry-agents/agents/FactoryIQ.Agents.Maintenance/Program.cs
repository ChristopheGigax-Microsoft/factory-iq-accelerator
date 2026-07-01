using FactoryIQ.Agents.Maintenance;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var config = ServiceRegistration.LoadConfigFromEnvironment();
var services = new ServiceCollection();
services.AddFoundryAgentServices(config);
services.AddSingleton<KnowledgeSearchService>();
services.AddSingleton<FabricDataAgentService>();
services.AddSingleton<MaintenanceAgent>();

await using var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
var agent = provider.GetRequiredService<MaintenanceAgent>();

logger.LogInformation("Starting Maintenance Agent...");

try
{
    await agent.InitializeAsync();

    // Example: correlate alarms for a machine
    var result = await agent.CorrelateAlarmsAsync("machine-001", TimeSpan.FromHours(24));
    logger.LogInformation("Correlation result:\n{Result}", result);
}
catch (Exception ex)
{
    logger.LogError(ex, "Maintenance Agent failed");
    Environment.ExitCode = 1;
}
