using FactoryIQ.Agents.ContinuousImprovement;
using FactoryIQ.Agents.Maintenance;
using FactoryIQ.Agents.Operations;
using FactoryIQ.Agents.PlantManager;
using FactoryIQ.Agents.Quality;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

try
{
    const string verifyOnlyArgument = "--verify-only";
    if (args.Length > 0
        && !(args.Length == 1
            && (string.Equals(args[0], AgentConsoleHost.RegisterOnlyArgument, StringComparison.OrdinalIgnoreCase)
                || string.Equals(args[0], verifyOnlyArgument, StringComparison.OrdinalIgnoreCase))))
    {
        throw new InvalidOperationException(
            $"Usage: dotnet run --project tools\\FactoryIQ.Agents.RegisterAll [-- {AgentConsoleHost.RegisterOnlyArgument}|{verifyOnlyArgument}]");
    }
    bool verifyOnly = args.Length == 1
        && string.Equals(args[0], verifyOnlyArgument, StringComparison.OrdinalIgnoreCase);

    var config = ServiceRegistration.LoadConfigFromEnvironment();
    if (config.Runtime != AgentRuntime.Cloud)
    {
        throw new InvalidOperationException("Register-all is only supported for AI_RUNTIME=cloud because it registers persistent Azure AI Foundry agents.");
    }

    var services = new ServiceCollection();
    services.AddFoundryAgentServices(config);
    services.AddSingleton<OperationsAgent>();
    services.AddSingleton<MaintenanceAgent>();
    services.AddSingleton<QualityAgent>();
    services.AddSingleton<PlantManagerAgent>();
    services.AddSingleton<ContinuousImprovementAgent>();

    using ServiceProvider provider = services.BuildServiceProvider();
    ILogger logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("RegisterAll");

    IFactoryAgent[] agents =
    [
        provider.GetRequiredService<OperationsAgent>(),
        provider.GetRequiredService<MaintenanceAgent>(),
        provider.GetRequiredService<QualityAgent>(),
        provider.GetRequiredService<PlantManagerAgent>(),
        provider.GetRequiredService<ContinuousImprovementAgent>(),
    ];

    foreach (IFactoryAgent agent in agents)
    {
        logger.LogInformation(verifyOnly ? "Verifying {AgentName}" : "Registering {AgentName}", agent.Name);
        if (verifyOnly)
        {
            await agent.VerifyAsync();
        }
        else
        {
            await agent.RegisterAsync();
        }
    }

    logger.LogInformation(
        verifyOnly
            ? "Verified {AgentCount} Factory IQ Foundry agents."
            : "Registered {AgentCount} Factory IQ Foundry agents.",
        agents.Length);
}
catch (Exception ex)
{
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.TimestampFormat = "HH:mm:ss ";
        });
    });
    loggerFactory.CreateLogger("RegisterAll").LogError(ex, "Factory IQ Foundry agent registration failed");
    Environment.ExitCode = 1;
}
