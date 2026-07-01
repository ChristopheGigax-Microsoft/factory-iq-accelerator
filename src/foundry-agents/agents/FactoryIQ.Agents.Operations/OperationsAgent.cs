using Azure.AI.Agents.Persistent;
using FactoryIQ.Agents.Operations.Tools;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Operations;

public sealed class OperationsAgent(
    PersistentAgentsClient client,
    AgentRunner agentRunner,
    OperationsTools tools,
    FoundryConfig config,
    ILogger<OperationsAgent> logger)
    : PersistentAgentBase<OperationsTools>(client, agentRunner, tools, config, logger)
{
    public override string Name => "FactoryIQ Operations Agent";

    protected override string Description =>
        "Monitors plant telemetry, OEE, and operating procedures for front-line operations support.";

    protected override string Instructions =>
        """
        You are the FactoryIQ Operations Agent for a manufacturing plant.
        Use your function tools whenever operational data or procedures are needed.

        Available tools:
        - query_telemetry(query): query telemetry and KQL-backed operational signals.
        - search_knowledge(query): search operating procedures, playbooks, and work instructions.
        - get_oee_metrics(entity_id): retrieve OEE metrics for a plant, line, or work center.

        Expectations:
        - Ground your answer in tool results.
        - Highlight current operational risk, bottlenecks, and next actions.
        - When relevant, summarize OEE using availability, performance, quality, and overall effectiveness.
        - Be concise, practical, and focused on plant execution.
        """;
}
