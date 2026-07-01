using System.Text.Json;
using Azure.AI.Agents.Persistent;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Services;

namespace FactoryIQ.Agents.Maintenance.Tools;

public sealed class MaintenanceTools(FabricDataAgentService fabricDataAgent, KnowledgeSearchService knowledgeSearch)
    : FunctionToolExecutorBase
{
    private readonly IReadOnlyList<ToolDefinition> _toolDefinitions =
    [
        CreateFunctionTool(
            "query_sensor_data",
            "Query sensor, alarm, and machine condition data from the plant telemetry store.",
            new ToolParameter("query", "The sensor or alarm question to answer.")),
        CreateFunctionTool(
            "search_maintenance_docs",
            "Search maintenance procedures, runbooks, and OEM documentation.",
            new ToolParameter("query", "The maintenance documentation search query.")),
        CreateFunctionTool(
            "get_asset_history",
            "Get maintenance and repair history for an asset.",
            new ToolParameter("asset_id", "The asset identifier.")),
    ];

    public override IReadOnlyList<ToolDefinition> ToolDefinitions => _toolDefinitions;

    public override async Task<string> InvokeAsync(RequiredFunctionToolCall toolCall, CancellationToken ct = default)
    {
        using JsonDocument args = JsonDocument.Parse(toolCall.Arguments);
        return toolCall.Name switch
        {
            "query_sensor_data" => await QuerySensorDataAsync(GetRequiredString(args.RootElement, "query"), ct),
            "search_maintenance_docs" => await SearchMaintenanceDocsAsync(GetRequiredString(args.RootElement, "query"), ct),
            "get_asset_history" => await GetAssetHistoryAsync(GetRequiredString(args.RootElement, "asset_id"), ct),
            _ => throw new InvalidOperationException($"Unsupported tool call: {toolCall.Name}"),
        };
    }

    public Task<string> QuerySensorDataAsync(string query, CancellationToken ct = default) =>
        fabricDataAgent.QueryAsync(query, ct);

    public async Task<string> SearchMaintenanceDocsAsync(string query, CancellationToken ct = default)
    {
        var results = await knowledgeSearch.SearchAsync(query, maxResults: 4, ct: ct);
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            results.Select(r => $"**{r.Title}** (score: {r.Score:F2}){Environment.NewLine}{r.Content}"));
    }

    public Task<string> GetAssetHistoryAsync(string assetId, CancellationToken ct = default) =>
        fabricDataAgent.QueryAsync(
            $"Get maintenance history, repairs, alarms, and work performed for asset {assetId}.",
            ct);
}
