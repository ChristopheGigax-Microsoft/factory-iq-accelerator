using System.Text.Json;
using Azure.AI.Agents.Persistent;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Services;

namespace FactoryIQ.Agents.Quality.Tools;

public sealed class QualityTools(FabricDataAgentService fabricDataAgent, KnowledgeSearchService knowledgeSearch)
    : FunctionToolExecutorBase
{
    private readonly IReadOnlyList<ToolDefinition> _toolDefinitions =
    [
        CreateFunctionTool(
            "query_quality_data",
            "Query inspection, SPC, defect, and scrap data from the plant telemetry store.",
            new ToolParameter("query", "The quality data question to answer.")),
        CreateFunctionTool(
            "search_quality_standards",
            "Search quality standards, procedures, and product specifications.",
            new ToolParameter("query", "The quality standards search query.")),
        CreateFunctionTool(
            "analyze_batch",
            "Analyze quality metrics and anomalies for a specific production batch.",
            new ToolParameter("batch_id", "The production batch identifier.")),
    ];

    public override IReadOnlyList<ToolDefinition> ToolDefinitions => _toolDefinitions;

    public override async Task<string> InvokeAsync(RequiredFunctionToolCall toolCall, CancellationToken ct = default)
    {
        using JsonDocument args = JsonDocument.Parse(toolCall.Arguments);
        return toolCall.Name switch
        {
            "query_quality_data" => await QueryQualityDataAsync(GetRequiredString(args.RootElement, "query"), ct),
            "search_quality_standards" => await SearchQualityStandardsAsync(GetRequiredString(args.RootElement, "query"), ct),
            "analyze_batch" => await AnalyzeBatchAsync(GetRequiredString(args.RootElement, "batch_id"), ct),
            _ => throw new InvalidOperationException($"Unsupported tool call: {toolCall.Name}"),
        };
    }

    public Task<string> QueryQualityDataAsync(string query, CancellationToken ct = default) =>
        fabricDataAgent.QueryAsync(query, ct);

    public async Task<string> SearchQualityStandardsAsync(string query, CancellationToken ct = default)
    {
        var results = await knowledgeSearch.SearchAsync(query, maxResults: 4, ct: ct);
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            results.Select(r => $"**{r.Title}** (score: {r.Score:F2}){Environment.NewLine}{r.Content}"));
    }

    public Task<string> AnalyzeBatchAsync(string batchId, CancellationToken ct = default) =>
        fabricDataAgent.QueryAsync(
            $"Analyze inspection results, SPC trends, scrap, and quality anomalies for batch {batchId}.",
            ct);
}
