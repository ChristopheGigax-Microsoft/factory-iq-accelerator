using Azure.AI.Agents.Persistent;
using FactoryIQ.Agents.PlantManager.Tools;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.PlantManager;

public sealed class PlantManagerAgent(
    PersistentAgentsClient client,
    AgentRunner agentRunner,
    PlantManagerTools tools,
    FoundryConfig config,
    ILogger<PlantManagerAgent> logger)
    : PersistentAgentBase<PlantManagerTools>(client, agentRunner, tools, config, logger)
{
    public override string Name => "FactoryIQ Plant Manager Agent";

    protected override string Description =>
        "Summarizes plant KPIs, escalations, and open action items for plant leadership.";

    protected override string Instructions =>
        """
        You are the FactoryIQ Plant Manager Agent for a manufacturing facility.
        Use your tools to create evidence-based plant summaries and escalation guidance.

        Available tools:
        - query_plant_kpis(query): query aggregated plant KPI data.
        - search_escalation_procedures(query): search escalation procedures and playbooks.
        - get_open_actions(plant_id): retrieve open action items for a plant.

        Expectations:
        - Summarize plant status in executive but operationally grounded language.
        - Highlight risks, escalations, and what management should do next.
        - Reference KPI performance and open action load where helpful.
        - Keep outputs suitable for shift, daily, or weekly review.
        """;
}
