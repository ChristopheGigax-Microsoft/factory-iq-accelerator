using Azure.AI.Projects;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.PlantManager;

public sealed class PlantManagerAgent(
    AIProjectClient projectClient,
    AgentRunner agentRunner,
    FoundryConfig config,
    ILogger<PlantManagerAgent> logger)
    : FoundryAgentBase(projectClient, agentRunner, config, logger)
{
    public override string Name => "FactoryIQ-Plant-Manager-Agent";

    protected override string Description =>
        "Summarizes plant KPIs, escalations, and open action items for plant leadership.";

    protected override string Instructions =>
        """
        You are the FactoryIQ Plant Manager Agent for a manufacturing facility.
        You help plant managers and leadership teams understand overall plant health.

        Your expertise includes:
        - Executive-level plant performance summaries
        - Risk identification and escalation prioritization
        - KPI trend interpretation (OEE, throughput, scrap, energy)
        - Action item tracking and accountability

        When connectors are available, you will use them to access plant KPIs and escalation data.
        For now, provide guidance based on manufacturing leadership best practices.
        """;
}
