using FactoryIQ.Agents.Quality;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var config = ServiceRegistration.LoadConfigFromEnvironment();
var services = new ServiceCollection();
services.AddFoundryAgentServices(config);
services.AddSingleton<KnowledgeSearchService>();
services.AddSingleton<FabricDataAgentService>();
services.AddSingleton<QualityAgent>();

await using var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
var agent = provider.GetRequiredService<QualityAgent>();

logger.LogInformation("Starting Quality Agent...");

try
{
    await agent.InitializeAsync();

    // Example: investigate a defect batch
    var result = await agent.InvestigateDefectAsync("batch-2024-1547", "surface-crack", "machine-003");
    logger.LogInformation("Investigation result:\n{Result}", result);
}
catch (Exception ex)
{
    logger.LogError(ex, "Quality Agent failed");
    Environment.ExitCode = 1;
}
