using FactoryIQ.Agents.PlantManager;
using FactoryIQ.Agents.PlantManager.Tools;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var config = ServiceRegistration.LoadConfigFromEnvironment();
var services = new ServiceCollection();
services.AddFoundryAgentServices(config);
services.AddSingleton<PlantManagerTools>();
services.AddSingleton<PlantManagerAgent>();

using var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
var agent = provider.GetRequiredService<PlantManagerAgent>();

try
{
    await AgentConsoleHost.RunAsync(agent, config, logger, args);
}
catch (Exception ex)
{
    logger.LogError(ex, "Plant Manager Agent failed");
    Environment.ExitCode = 1;
}
