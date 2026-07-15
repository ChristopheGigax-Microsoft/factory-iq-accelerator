using FactoryIQ.Agents.ContinuousImprovement;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var config = ServiceRegistration.LoadConfigFromEnvironment();
var services = new ServiceCollection();
services.AddFoundryAgentServices(config);
services.AddSingleton<ContinuousImprovementAgent>();

using var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
var agent = provider.GetRequiredService<ContinuousImprovementAgent>();

try
{
    await AgentConsoleHost.RunAsync(agent, config, logger, args);
}
catch (Exception ex)
{
    logger.LogError(ex, "Continuous Improvement Agent failed");
    Environment.ExitCode = 1;
}
