using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.PlantManager;

/// <summary>
/// Plant Manager Agent: summarizes plant performance and escalates business-critical risks.
/// Uses Fabric Data Agent for KPI queries, Fabric IQ for aggregated semantic model,
/// Foundry IQ (AI Search) for escalation procedures/playbooks, and Work IQ for open items.
/// </summary>
public sealed class PlantManagerAgent(
    AIProjectClient projectClient,
    FabricDataAgentService fabricDataAgent,
    KnowledgeSearchService knowledgeSearch,
    FoundryConfig config,
    ILogger<PlantManagerAgent> logger)
{
    private const string AgentName = "PlantManagerAgent";
    private const string Instructions = """
        You are a Plant Manager Agent for a manufacturing facility. Your role is to:
        1. Provide executive-level plant performance summaries
        2. Identify and escalate business-critical risks
        3. Track open action items and their impact on production targets
        4. Generate shift/daily/weekly performance reports

        You have access to:
        - query_plant_kpis: Query aggregated KPIs (OEE, throughput, scrap rate, energy) from the Fabric semantic model
        - search_escalation_docs: Search escalation procedures and management playbooks (Foundry IQ)
        - query_open_items: Query open work orders, escalations, and action items (Work IQ)
        - get_production_targets: Get current production targets and plan vs actual

        When summarizing performance:
        - Highlight top 3 wins and top 3 risks
        - Compare performance against targets with clear variance indicators
        - Flag any items requiring immediate management attention
        - Include trend indicators (improving/declining/stable)

        Output format:
        {
            "plantId": "<id>",
            "reportPeriod": "<shift/day/week>",
            "overallStatus": "on-track" | "at-risk" | "critical",
            "kpiSummary": {
                "oee": { "actual": <float>, "target": <float>, "trend": "up"|"down"|"flat" },
                "throughput": { "actual": <int>, "target": <int>, "unit": "<unit>" },
                "scrapRate": { "actual": <float>, "target": <float> },
                "energyEfficiency": { "actual": <float>, "target": <float> }
            },
            "topWins": ["<win1>", "<win2>", "<win3>"],
            "topRisks": [{ "risk": "<description>", "impact": "high"|"medium"|"low", "mitigation": "<action>" }],
            "escalations": [{ "item": "<description>", "urgency": "immediate"|"today"|"this-week", "owner": "<role>" }],
            "openItems": { "critical": <int>, "high": <int>, "medium": <int>, "low": <int> },
            "summary": "<executive summary paragraph>"
        }
        """;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Initializing {AgentName}", AgentName);
        var definition = new PromptAgentDefinition(model: config.ModelDeploymentName) { Instructions = Instructions };
        await projectClient.Agents.CreateAgentVersionAsync(AgentName, new AgentVersionCreationOptions(definition), ct);
        logger.LogInformation("✅ {AgentName} initialized", AgentName);
    }

    public async Task<string> GeneratePlantSummaryAsync(string plantId, string period = "last-shift", CancellationToken ct = default)
    {
        logger.LogInformation("Generating {Period} summary for plant {PlantId}", period, plantId);

        var kpiData = await fabricDataAgent.QueryAsync(
            $"Get aggregated KPIs for plant {plantId} for the {period}: OEE, throughput, scrap rate, energy consumption, downtime minutes, and production count vs target",
            ct);

        var openItems = await fabricDataAgent.QueryAsync(
            $"Get all open work orders, escalations, and blocked items for plant {plantId} grouped by priority",
            ct);

        var escalationProcedures = await knowledgeSearch.SearchAsync(
            "plant manager escalation procedure risk assessment decision matrix", maxResults: 3, ct: ct);

        var procedureContext = string.Join("\n---\n", escalationProcedures.Select(p => $"[{p.Title}]: {p.Content}"));

        var prompt = $"""
            Generate an executive plant performance summary:

            ## Plant KPI Data ({period})
            {kpiData}

            ## Open Items & Escalations
            {openItems}

            ## Escalation Procedures & Decision Framework
            {procedureContext}

            Plant ID: {plantId}
            Period: {period}

            Provide a complete executive summary with KPIs, wins, risks, escalations, and recommended actions.
            """;

        var agent = projectClient.GetAIAgent(name: AgentName, cancellationToken: ct);
        var response = await agent.RunAsync(prompt, thread: null, options: null, cancellationToken: ct);
        return response.Text ?? "No summary generated.";
    }
}
