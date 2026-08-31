using FactoryIQ.Agents.PlantManager;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Local;
using FactoryIQ.Agents.Shared.Local.Tools.OpcUa;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var config = ServiceRegistration.LoadConfigFromEnvironment();
var services = new ServiceCollection();
if (config.Runtime == AgentRuntime.Local)
{
    services.AddLocalAgentServices(config);
}
else
{
    services.AddFoundryAgentServices(config);
    services.AddSingleton<PlantManagerAgent>();
}

using var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
IFactoryAgent agent = config.Runtime == AgentRuntime.Local
    ? new LocalFactoryAgent(
        FactoryAgentProfiles.PlantManager,
        provider.GetRequiredService<LocalModelRuntime>(),
        provider.GetRequiredService<ILogger<LocalFactoryAgent>>(),
        provider.GetRequiredService<OpcUaMachineDataTool>())
    : provider.GetRequiredService<PlantManagerAgent>();

try
{
    await AgentConsoleHost.RunAsync(agent, config, logger, args);
}
catch (Exception ex)
{
    logger.LogError(ex, "Plant Manager Agent failed");
    Environment.ExitCode = 1;
}
