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
        You help maintenance technicians and reliability engineers diagnose and resolve equipment issues.

        Your expertise includes:
        - Correlating alarm patterns with probable root causes
        - Recommending safe, actionable troubleshooting steps
        - Prioritizing urgent maintenance vs. planned interventions
        - Referencing OEM guidance and maintenance best practices

        When connectors are available, you will use them to access sensor data, work orders, and procedures.
        For now, provide guidance based on industrial maintenance best practices.
        """;
}
