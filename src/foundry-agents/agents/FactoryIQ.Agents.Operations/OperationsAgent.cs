using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Operations;

/// <summary>
/// Operations Agent: monitors plant performance, detects deviations, and explains likely causes.
/// Uses Fabric Data Agent for KQL telemetry, Fabric IQ for OEE semantic model,
/// and Foundry IQ (AI Search) for OEE procedure documentation.
/// </summary>
public sealed class OperationsAgent(
    AIProjectClient projectClient,
    FabricDataAgentService fabricDataAgent,
    KnowledgeSearchService knowledgeSearch,
    FoundryConfig config,
    ILogger<OperationsAgent> logger)
{
    private const string AgentName = "OperationsAgent";
    private const string Instructions = """
        You are an Operations Agent for a manufacturing plant. Your role is to:
        1. Monitor Overall Equipment Effectiveness (OEE) and key performance indicators
        2. Detect deviations from expected performance baselines
        3. Explain likely root causes of performance degradation
        4. Recommend immediate corrective actions

        You have access to:
        - query_telemetry: Query real-time and historical sensor/machine data from the plant's KQL database
        - search_knowledge: Search operational procedures, OEE playbooks, and deviation response guides
        - get_oee_metrics: Retrieve current OEE breakdown (Availability, Performance, Quality)

        When analyzing deviations:
        - Compare current metrics against baseline thresholds
        - Identify which OEE component (A, P, Q) is most impacted
        - Search knowledge base for known patterns matching the deviation
        - Provide a structured response with severity, impacted area, likely cause, and recommended action

        Output format:
        {
            "status": "normal" | "warning" | "critical",
            "oee": { "availability": <float>, "performance": <float>, "quality": <float>, "overall": <float> },
            "deviations": [{ "metric": "<name>", "current": <value>, "baseline": <value>, "severity": "<level>" }],
            "likelyCauses": ["<cause1>", "<cause2>"],
            "recommendedActions": ["<action1>", "<action2>"],
            "summary": "<human-readable summary>"
        }
        """;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Initializing {AgentName} with model {Model}", AgentName, config.ModelDeploymentName);

        var definition = new PromptAgentDefinition(model: config.ModelDeploymentName) { Instructions = Instructions };
        await projectClient.Agents.CreateAgentVersionAsync(AgentName, new AgentVersionCreationOptions(definition), ct);

        logger.LogInformation("✅ {AgentName} initialized", AgentName);
    }

    public async Task<string> AnalyzePlantPerformanceAsync(string plantId, CancellationToken ct = default)
    {
        logger.LogInformation("Analyzing performance for plant {PlantId}", plantId);

        // Gather telemetry data from Fabric Data Agent
        var telemetryData = await fabricDataAgent.QueryAsync(
            $"Get the latest OEE metrics and machine status for all work centers in plant {plantId} for the last 4 hours",
            ct);

        // Search knowledge base for deviation response procedures
        var procedures = await knowledgeSearch.SearchAsync(
            "OEE deviation detection and response procedures", maxResults: 3, ct: ct);

        var knowledgeContext = string.Join("\n---\n", procedures.Select(p => $"[{p.Title}]: {p.Content}"));

        // Build prompt and invoke the agent
        var prompt = $"""
            Analyze the following plant performance data and identify any deviations:

            ## Current Telemetry Data
            {telemetryData}

            ## Reference Procedures
            {knowledgeContext}

            Provide a complete analysis with OEE breakdown, deviations detected, likely causes, and recommended actions.
            Plant ID: {plantId}
            """;

        var agent = projectClient.GetAIAgent(name: AgentName, cancellationToken: ct);
        var response = await agent.RunAsync(prompt, thread: null, options: null, cancellationToken: ct);

        return response.Text ?? "No analysis generated.";
    }
}
