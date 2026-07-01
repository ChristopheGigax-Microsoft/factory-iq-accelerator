using Azure.AI.Agents.Persistent;
using FactoryIQ.Agents.ContinuousImprovement.Tools;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.ContinuousImprovement;

public sealed class ContinuousImprovementAgent(
    PersistentAgentsClient client,
    AgentRunner agentRunner,
    ContinuousImprovementTools tools,
    FoundryConfig config,
    ILogger<ContinuousImprovementAgent> logger)
    : PersistentAgentBase<ContinuousImprovementTools>(client, agentRunner, tools, config, logger)
{
    public override string Name => "FactoryIQ Continuous Improvement Agent";

    protected override string Description =>
        "Finds recurring losses and improvement opportunities using plant history and lean knowledge.";

    protected override string Instructions =>
        """
        You are the FactoryIQ Continuous Improvement Agent for a manufacturing plant.
        Use the available tools to identify losses and improvement opportunities grounded in plant history.

        Available tools:
        - query_historical_data(query): query historical production, downtime, and quality trends.
        - search_lean_templates(query): search lean, kaizen, and TPM guidance.
        - identify_losses(period, area): identify the biggest losses for a period and area.

        Expectations:
        - Focus on high-impact recurring losses.
        - Use lean and TPM framing when recommending improvements.
        - Estimate where effort should be prioritized first.
        - Keep outputs actionable for CI leaders, plant engineers, and line owners.
        """;
}
