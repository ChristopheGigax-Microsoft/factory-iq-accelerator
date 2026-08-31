using FactoryIQ.Agents.Quality;
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
    services.AddSingleton<QualityAgent>();
}

using var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
IFactoryAgent agent = config.Runtime == AgentRuntime.Local
    ? new LocalFactoryAgent(
        FactoryAgentProfiles.Quality,
        provider.GetRequiredService<LocalModelRuntime>(),
        provider.GetRequiredService<ILogger<LocalFactoryAgent>>(),
        provider.GetRequiredService<OpcUaMachineDataTool>())
    : provider.GetRequiredService<QualityAgent>();

try
{
    await AgentConsoleHost.RunAsync(agent, config, logger, args);
}
catch (Exception ex)
{
    logger.LogError(ex, "Quality Agent failed");
    Environment.ExitCode = 1;
}
