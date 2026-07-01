using FactoryIQ.Agents.Operations;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var config = ServiceRegistration.LoadConfigFromEnvironment();
var services = new ServiceCollection();
services.AddFoundryAgentServices(config);
services.AddSingleton<KnowledgeSearchService>();
services.AddSingleton<FabricDataAgentService>();
services.AddSingleton<OperationsAgent>();

await using var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
var agent = provider.GetRequiredService<OperationsAgent>();

logger.LogInformation("Starting Operations Agent...");

try
{
    await agent.InitializeAsync();

    // Example: monitor current plant performance
    var result = await agent.AnalyzePlantPerformanceAsync("plant-001");
    logger.LogInformation("Analysis result:\n{Result}", result);
}
catch (Exception ex)
{
    logger.LogError(ex, "Operations Agent failed");
    Environment.ExitCode = 1;
}
