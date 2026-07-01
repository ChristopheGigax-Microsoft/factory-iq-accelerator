using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.ContinuousImprovement;

/// <summary>
/// Continuous Improvement Agent: identifies recurring losses and improvement opportunities.
/// Uses Fabric Data Agent for historical KQL analysis, Foundry IQ (AI Search) for lean/kaizen
/// templates and best practices, and Web IQ for industry benchmarks.
/// </summary>
public sealed class ContinuousImprovementAgent(
    AIProjectClient projectClient,
    FabricDataAgentService fabricDataAgent,
    KnowledgeSearchService knowledgeSearch,
    FoundryConfig config,
    ILogger<ContinuousImprovementAgent> logger)
{
    private const string AgentName = "ContinuousImprovementAgent";
    private const string Instructions = """
        You are a Continuous Improvement Agent for a manufacturing plant. Your role is to:
        1. Identify recurring production losses (the "chronic" problems)
        2. Analyze patterns in downtime, scrap, speed losses, and minor stops
        3. Propose structured improvement opportunities using lean/TPM methodology
        4. Prioritize opportunities by estimated impact and feasibility

        You have access to:
        - query_historical_data: Query historical production, downtime, and quality data from KQL
        - search_lean_templates: Search lean/kaizen templates, TPM guides, and improvement frameworks (Foundry IQ)
        - search_benchmarks: Search industry benchmarks and best practices (Web IQ)
        - get_loss_pareto: Get Pareto analysis of losses by category

        When identifying opportunities:
        - Apply Pareto principle (80/20 rule) to focus on biggest losses
        - Categorize losses using Six Big Losses framework (TPM)
        - Cross-reference with lean templates for structured improvement approaches
        - Estimate potential impact in OEE points or production hours recovered
        - Consider feasibility (effort, investment, timeline)

        Output format:
        {
            "analysisScope": { "plantId": "<id>", "period": "<timeframe>" },
            "lossBreakdown": {
                "totalLostHours": <float>,
                "byCategory": [{ "category": "<name>", "hours": <float>, "percent": <float> }]
            },
            "opportunities": [
                {
                    "title": "<improvement title>",
                    "category": "availability" | "performance" | "quality",
                    "lossType": "<six big loss type>",
                    "description": "<what to improve and why>",
                    "estimatedImpact": { "oeePoints": <float>, "hoursRecovered": <float>, "annualSaving": "<estimate>" },
                    "methodology": "<lean tool/approach>",
                    "effort": "low" | "medium" | "high",
                    "priority": "quick-win" | "major-project" | "strategic",
                    "nextSteps": ["<step1>", "<step2>"]
                }
            ],
            "summary": "<executive summary of top improvement opportunities>"
        }
        """;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Initializing {AgentName}", AgentName);
        var definition = new PromptAgentDefinition(model: config.ModelDeploymentName) { Instructions = Instructions };
        await projectClient.Agents.CreateAgentVersionAsync(AgentName, new AgentVersionCreationOptions(definition), ct);
        logger.LogInformation("✅ {AgentName} initialized", AgentName);
    }

    public async Task<string> IdentifyOpportunitiesAsync(string plantId, string period, CancellationToken ct = default)
    {
        logger.LogInformation("Identifying improvement opportunities for {PlantId} over {Period}", plantId, period);

        var historicalData = await fabricDataAgent.QueryAsync(
            $"Get loss analysis for plant {plantId} over {period}: downtime by reason code, scrap by defect type, speed losses by machine, minor stops frequency. Include Pareto ranking.",
            ct);

        var leanTemplates = await knowledgeSearch.SearchAsync(
            "lean manufacturing kaizen improvement TPM six big losses methodology", maxResults: 4, ct: ct);

        var templateContext = string.Join("\n---\n", leanTemplates.Select(p => $"[{p.Title}]: {p.Content}"));

        var prompt = $"""
            Analyze production losses and identify improvement opportunities:

            ## Historical Loss Data ({period})
            {historicalData}

            ## Lean/Kaizen Methodology & Templates
            {templateContext}

            Plant ID: {plantId}
            Analysis Period: {period}

            Apply Pareto analysis, categorize by Six Big Losses, and propose prioritized improvement
            opportunities with estimated impact and recommended lean methodology.
            """;

        var agent = projectClient.GetAIAgent(name: AgentName, cancellationToken: ct);
        var response = await agent.RunAsync(prompt, thread: null, options: null, cancellationToken: ct);
        return response.Text ?? "No opportunities identified.";
    }
}
