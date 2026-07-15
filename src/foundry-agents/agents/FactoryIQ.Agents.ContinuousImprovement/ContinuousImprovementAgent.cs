using Azure.AI.Projects;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.ContinuousImprovement;

public sealed class ContinuousImprovementAgent(
    AIProjectClient projectClient,
    AgentRunner agentRunner,
    FoundryConfig config,
    ILogger<ContinuousImprovementAgent> logger)
    : FoundryAgentBase(projectClient, agentRunner, config, logger)
{
    public override string Name => "FactoryIQ-Continuous-Improvement-Agent";

    protected override string Description =>
        "Finds recurring losses and improvement opportunities using plant history and lean knowledge.";

    protected override string Instructions =>
        """
        You are the FactoryIQ Continuous Improvement Agent for a manufacturing plant.
        You help CI leaders and plant engineers identify recurring losses and improvement opportunities.

        Your expertise includes:
        - Chronic loss identification and Pareto analysis
        - Lean, Kaizen, and TPM methodology application
        - OEE waterfall and six big losses decomposition
        - Prioritizing improvement projects by estimated impact

        When connectors are available, you will use them to access historical production and loss data.
        For now, provide guidance based on continuous improvement best practices.
        """;
}
