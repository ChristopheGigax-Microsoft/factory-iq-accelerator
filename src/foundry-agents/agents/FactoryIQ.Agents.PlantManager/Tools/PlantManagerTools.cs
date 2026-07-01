using System.Text.Json;
using Azure.AI.Agents.Persistent;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Services;

namespace FactoryIQ.Agents.PlantManager.Tools;

public sealed class PlantManagerTools(FabricDataAgentService fabricDataAgent, KnowledgeSearchService knowledgeSearch)
    : FunctionToolExecutorBase
{
    private readonly IReadOnlyList<ToolDefinition> _toolDefinitions =
    [
        CreateFunctionTool(
            "query_plant_kpis",
            "Query aggregated plant KPIs such as OEE, throughput, scrap, and energy usage.",
            new ToolParameter("query", "The KPI question to answer.")),
        CreateFunctionTool(
            "search_escalation_procedures",
            "Search escalation procedures, management playbooks, and response guidance.",
            new ToolParameter("query", "The escalation procedure search query.")),
        CreateFunctionTool(
            "get_open_actions",
            "Get open action items and escalations for a plant.",
            new ToolParameter("plant_id", "The plant identifier.")),
    ];

    public override IReadOnlyList<ToolDefinition> ToolDefinitions => _toolDefinitions;

    public override async Task<string> InvokeAsync(RequiredFunctionToolCall toolCall, CancellationToken ct = default)
    {
        using JsonDocument args = JsonDocument.Parse(toolCall.Arguments);
        return toolCall.Name switch
        {
            "query_plant_kpis" => await QueryPlantKpisAsync(GetRequiredString(args.RootElement, "query"), ct),
            "search_escalation_procedures" => await SearchEscalationProceduresAsync(GetRequiredString(args.RootElement, "query"), ct),
            "get_open_actions" => await GetOpenActionsAsync(GetRequiredString(args.RootElement, "plant_id"), ct),
            _ => throw new InvalidOperationException($"Unsupported tool call: {toolCall.Name}"),
        };
    }

    public Task<string> QueryPlantKpisAsync(string query, CancellationToken ct = default) =>
        fabricDataAgent.QueryAsync(query, ct);

    public async Task<string> SearchEscalationProceduresAsync(string query, CancellationToken ct = default)
    {
        var results = await knowledgeSearch.SearchAsync(query, maxResults: 3, ct: ct);
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            results.Select(r => $"**{r.Title}** (score: {r.Score:F2}){Environment.NewLine}{r.Content}"));
    }

    public Task<string> GetOpenActionsAsync(string plantId, CancellationToken ct = default) =>
        fabricDataAgent.QueryAsync(
            $"Get open action items, work orders, escalations, and blocked items for plant {plantId}.",
            ct);
}
