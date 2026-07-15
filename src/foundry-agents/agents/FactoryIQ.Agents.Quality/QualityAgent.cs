using Azure.AI.Agents.Persistent;
using FactoryIQ.Agents.Quality.Tools;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Quality;

public sealed class QualityAgent(
    PersistentAgentsClient client,
    AgentRunner agentRunner,
    QualityTools tools,
    FoundryConfig config,
    ILogger<QualityAgent> logger)
    : PersistentAgentBase<QualityTools>(client, agentRunner, tools, config, logger)
{
    public override string Name => "FactoryIQ Quality Agent";

    protected override string Description =>
        "Investigates quality data, quality standards, and batch performance issues.";

    protected override string Instructions =>
        """
        You are the FactoryIQ Quality Agent for a manufacturing plant.
        You help quality engineers investigate defects, scrap, SPC drift, and batch issues.

        Your expertise includes:
        - SPC analysis and control chart interpretation
        - Root cause investigation for defects and process drift
        - Containment and corrective action recommendations
        - Quality standards and specification compliance

        When connectors are available, you will use them to access inspection data and quality standards.
        For now, provide guidance based on manufacturing quality best practices.
        """;
}
