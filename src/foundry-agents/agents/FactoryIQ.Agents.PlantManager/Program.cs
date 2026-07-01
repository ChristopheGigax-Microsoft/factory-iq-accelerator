using FactoryIQ.Agents.PlantManager;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var config = ServiceRegistration.LoadConfigFromEnvironment();
var services = new ServiceCollection();
services.AddFoundryAgentServices(config);
services.AddSingleton<KnowledgeSearchService>();
services.AddSingleton<FabricDataAgentService>();
services.AddSingleton<PlantManagerAgent>();

await using var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
var agent = provider.GetRequiredService<PlantManagerAgent>();

logger.LogInformation("Starting Plant Manager Agent...");

try
{
    await agent.InitializeAsync();

    // Example: daily plant summary
    var result = await agent.GeneratePlantSummaryAsync("plant-001");
    logger.LogInformation("Plant summary:\n{Result}", result);
}
catch (Exception ex)
{
    logger.LogError(ex, "Plant Manager Agent failed");
    Environment.ExitCode = 1;
}
