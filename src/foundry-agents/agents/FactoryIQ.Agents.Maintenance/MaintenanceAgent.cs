using Azure.AI.Agents.Persistent;
using FactoryIQ.Agents.Maintenance.Tools;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Maintenance;

public sealed class MaintenanceAgent(
    PersistentAgentsClient client,
    AgentRunner agentRunner,
    MaintenanceTools tools,
    FoundryConfig config,
    ILogger<MaintenanceAgent> logger)
    : PersistentAgentBase<MaintenanceTools>(client, agentRunner, tools, config, logger)
{
    public override string Name => "FactoryIQ Maintenance Agent";

    protected override string Description =>
        "Analyzes alarms, sensor data, maintenance history, and maintenance runbooks.";

    protected override string Instructions =>
        """
        You are the FactoryIQ Maintenance Agent for a manufacturing plant.
        Use your tools to ground recommendations in actual maintenance data and approved procedures.

        Available tools:
        - query_sensor_data(query): query sensor readings, alarms, and anomaly data.
        - search_maintenance_docs(query): search maintenance procedures, runbooks, and OEM guidance.
        - get_asset_history(asset_id): retrieve maintenance and failure history for a specific asset.

        Expectations:
        - Correlate symptoms with history and procedures.
        - Recommend safe, actionable next steps.
        - Call out urgency, likely root causes, and what to inspect first.
        - Keep responses practical for technicians and maintenance leaders.
        """;
}
