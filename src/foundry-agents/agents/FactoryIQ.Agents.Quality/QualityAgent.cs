using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Quality;

/// <summary>
/// Quality Agent: investigates scrap, defects, process drift, and batch issues.
/// Uses Fabric Data Agent for quality KQL data, Foundry IQ (AI Search) for quality standards
/// and SPC documentation, and Web IQ for supplier specifications lookup.
/// </summary>
public sealed class QualityAgent(
    AIProjectClient projectClient,
    FabricDataAgentService fabricDataAgent,
    KnowledgeSearchService knowledgeSearch,
    FoundryConfig config,
    ILogger<QualityAgent> logger)
{
    private const string AgentName = "QualityAgent";
    private const string Instructions = """
        You are a Quality Agent for a manufacturing plant. Your role is to:
        1. Investigate scrap events and quality defects
        2. Detect process drift using SPC (Statistical Process Control) principles
        3. Analyze batch-level quality issues
        4. Trace defects to root causes in process parameters

        You have access to:
        - query_quality_data: Query quality inspection results, scrap records, and SPC data from KQL
        - search_quality_standards: Search quality standards, SPC reference docs, and inspection criteria (Foundry IQ)
        - search_web: Search external supplier specifications and industry quality benchmarks (Web IQ)
        - get_process_parameters: Get process parameter history for a specific batch/machine

        When investigating defects:
        - Correlate defect timing with process parameter changes
        - Check if the defect pattern matches known failure modes in quality standards
        - Look for raw material or supplier-related patterns
        - Apply SPC rules (Nelson rules, Western Electric rules) for drift detection

        Output format:
        {
            "batchId": "<id>",
            "defectType": "<type>",
            "investigation": {
                "rootCause": "<identified or suspected cause>",
                "confidence": <0-1>,
                "contributingFactors": ["<factor1>", "<factor2>"],
                "processParameters": [{ "name": "<param>", "deviation": "<description>" }]
            },
            "spcAnalysis": {
                "driftDetected": true|false,
                "violatedRules": ["<rule>"],
                "trendDirection": "increasing" | "decreasing" | "stable"
            },
            "containmentActions": ["<action1>", "<action2>"],
            "preventiveActions": ["<action1>", "<action2>"],
            "summary": "<human-readable summary>"
        }
        """;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Initializing {AgentName}", AgentName);
        var definition = new PromptAgentDefinition(model: config.ModelDeploymentName) { Instructions = Instructions };
        await projectClient.Agents.CreateAgentVersionAsync(AgentName, new AgentVersionCreationOptions(definition), ct);
        logger.LogInformation("✅ {AgentName} initialized", AgentName);
    }

    public async Task<string> InvestigateDefectAsync(string batchId, string defectType, string machineId, CancellationToken ct = default)
    {
        logger.LogInformation("Investigating defect {DefectType} in batch {BatchId} on {MachineId}", defectType, batchId, machineId);

        var qualityData = await fabricDataAgent.QueryAsync(
            $"Get quality inspection results, scrap records, and SPC chart data for batch {batchId} on machine {machineId}, including process parameters during production",
            ct);

        var standards = await knowledgeSearch.SearchAsync(
            $"quality standard defect investigation {defectType} SPC analysis", maxResults: 4, ct: ct);

        var standardsContext = string.Join("\n---\n", standards.Select(p => $"[{p.Title}]: {p.Content}"));

        var prompt = $"""
            Investigate the following quality defect:

            ## Defect Details
            - Batch ID: {batchId}
            - Defect Type: {defectType}
            - Machine: {machineId}

            ## Quality Data & Process Parameters
            {qualityData}

            ## Quality Standards & SPC References
            {standardsContext}

            Perform a thorough investigation: identify root cause, check for process drift (SPC),
            and recommend containment and preventive actions.
            """;

        var agent = projectClient.GetAIAgent(name: AgentName, cancellationToken: ct);
        var response = await agent.RunAsync(prompt, thread: null, options: null, cancellationToken: ct);
        return response.Text ?? "No investigation result generated.";
    }
}
