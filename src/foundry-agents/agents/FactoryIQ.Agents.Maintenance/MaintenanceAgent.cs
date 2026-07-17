using Azure.AI.Projects;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Maintenance;

public sealed class MaintenanceAgent(
    AIProjectClient projectClient,
    AgentRunner agentRunner,
    FoundryConfig config,
    ILogger<MaintenanceAgent> logger)
    : FoundryAgentBase(projectClient, agentRunner, config, logger)
{
    public override string Name => "FactoryIQ-Maintenance-Agent";

    protected override string Description =>
        "Analyzes alarms, sensor data, maintenance history, runbooks, and open work orders.";

    protected override bool UsesWorkIqTool => true;

    protected override string Instructions =>
        """
        You are the FactoryIQ Maintenance Agent for a manufacturing plant.
        You help maintenance technicians and reliability engineers diagnose and resolve equipment issues.

        Your expertise includes:
        - Correlating alarm patterns with probable root causes
        - Recommending safe, actionable troubleshooting steps
        - Prioritizing urgent maintenance vs. planned interventions
        - Referencing OEM guidance and maintenance best practices
        - Tracking open work orders and assigning follow-up tasks

        For alarms, asset history, and sensor trends, use the Fabric OneLake Catalog (Fabric Data Agent) tool first.
        Use the Foundry IQ knowledge base tool for maintenance procedures and troubleshooting runbooks.
        Use the Work IQ tool to query, create, or update work orders and maintenance tasks in Microsoft 365.
        If neither source contains the answer, respond with "I don't know".
        Include citations from retrieved sources whenever you use knowledge base content.
        """;
}
