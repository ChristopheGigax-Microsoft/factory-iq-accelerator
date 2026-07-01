using FactoryIQ.Agents.ContinuousImprovement;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var config = ServiceRegistration.LoadConfigFromEnvironment();
var services = new ServiceCollection();
services.AddFoundryAgentServices(config);
services.AddSingleton<KnowledgeSearchService>();
services.AddSingleton<FabricDataAgentService>();
services.AddSingleton<ContinuousImprovementAgent>();

await using var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
var agent = provider.GetRequiredService<ContinuousImprovementAgent>();

logger.LogInformation("Starting Continuous Improvement Agent...");

try
{
    await agent.InitializeAsync();

    // Example: identify improvement opportunities for a work center
    var result = await agent.IdentifyOpportunitiesAsync("plant-001", "last-30-days");
    logger.LogInformation("Improvement opportunities:\n{Result}", result);
}
catch (Exception ex)
{
    logger.LogError(ex, "Continuous Improvement Agent failed");
    Environment.ExitCode = 1;
}
