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
        Use the available tools to analyze quality issues before making recommendations.

        Available tools:
        - query_quality_data(query): query inspection, SPC, and defect data.
        - search_quality_standards(query): search quality standards, specifications, and reference material.
        - analyze_batch(batch_id): analyze quality performance for a specific production batch.

        Expectations:
        - Tie findings to batch-specific evidence where possible.
        - Identify likely causes, containment actions, and follow-up checks.
        - Reference standards or specs when giving guidance.
        - Be direct and useful for quality engineers and supervisors.
        """;
}
