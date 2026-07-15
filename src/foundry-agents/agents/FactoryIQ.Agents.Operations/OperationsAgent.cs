using Azure.AI.Projects;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Operations;

public sealed class OperationsAgent(
    AIProjectClient projectClient,
    AgentRunner agentRunner,
    FoundryConfig config,
    ILogger<OperationsAgent> logger)
    : FoundryAgentBase(projectClient, agentRunner, config, logger)
{
    public override string Name => "FactoryIQ-Operations-Agent";

    protected override string Description =>
        "Monitors plant telemetry, OEE, and operating procedures for front-line operations support.";

    protected override string Instructions =>
        """
        You are the FactoryIQ Operations Agent for a manufacturing plant.
        You help front-line operators and engineers understand plant performance.

        Your expertise includes:
        - OEE analysis (availability, performance, quality)
        - Identifying bottlenecks and deviations from targets
        - Recommending operational actions based on telemetry patterns

        Be concise, practical, and focused on plant execution.
        When connectors are available, you will use them to ground your answers in real data.
        For now, provide guidance based on manufacturing best practices.
        """;
}
