using System.Text.Json;
using Azure.AI.Agents.Persistent;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Services;

namespace FactoryIQ.Agents.Operations.Tools;

public sealed class OperationsTools(FabricDataAgentService fabricDataAgent, KnowledgeSearchService knowledgeSearch)
    : FunctionToolExecutorBase
{
    private readonly IReadOnlyList<ToolDefinition> _toolDefinitions =
    [
        CreateFunctionTool(
            "query_telemetry",
            "Query real-time or historical plant telemetry through the Fabric Data Agent.",
            new ToolParameter("query", "The telemetry or KQL-style question to answer.")),
        CreateFunctionTool(
            "search_knowledge",
            "Search operational procedures, playbooks, and operating guidance in Azure AI Search.",
            new ToolParameter("query", "The procedure or knowledge base search query.")),
        CreateFunctionTool(
            "get_oee_metrics",
            "Get OEE metrics for a plant, line, or work center.",
            new ToolParameter("entity_id", "The plant, line, or work center identifier.")),
    ];

    public override IReadOnlyList<ToolDefinition> ToolDefinitions => _toolDefinitions;

    public override async Task<string> InvokeAsync(RequiredFunctionToolCall toolCall, CancellationToken ct = default)
    {
        using JsonDocument args = JsonDocument.Parse(toolCall.Arguments);
        return toolCall.Name switch
        {
            "query_telemetry" => await QueryTelemetryAsync(GetRequiredString(args.RootElement, "query"), ct),
            "search_knowledge" => await SearchKnowledgeAsync(GetRequiredString(args.RootElement, "query"), ct),
            "get_oee_metrics" => await GetOeeMetricsAsync(GetRequiredString(args.RootElement, "entity_id"), ct),
            _ => throw new InvalidOperationException($"Unsupported tool call: {toolCall.Name}"),
        };
    }

    public Task<string> QueryTelemetryAsync(string query, CancellationToken ct = default) =>
        fabricDataAgent.QueryAsync(query, ct);

    public async Task<string> SearchKnowledgeAsync(string query, CancellationToken ct = default)
    {
        var results = await knowledgeSearch.SearchAsync(query, maxResults: 3, ct: ct);
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            results.Select(r => $"**{r.Title}** (score: {r.Score:F2}){Environment.NewLine}{r.Content}"));
    }

    public Task<string> GetOeeMetricsAsync(string entityId, CancellationToken ct = default) =>
        fabricDataAgent.QueryAsync(
            $"Get current OEE for entity {entityId}, including availability, performance, quality, and overall effectiveness.",
            ct);
}
