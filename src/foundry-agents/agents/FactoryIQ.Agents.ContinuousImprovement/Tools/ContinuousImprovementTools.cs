using System.Text.Json;
using Azure.AI.Agents.Persistent;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Services;

namespace FactoryIQ.Agents.ContinuousImprovement.Tools;

public sealed class ContinuousImprovementTools(FabricDataAgentService fabricDataAgent, KnowledgeSearchService knowledgeSearch)
    : FunctionToolExecutorBase
{
    private readonly IReadOnlyList<ToolDefinition> _toolDefinitions =
    [
        CreateFunctionTool(
            "query_historical_data",
            "Query historical production, downtime, scrap, and loss data.",
            new ToolParameter("query", "The historical data question to answer.")),
        CreateFunctionTool(
            "search_lean_templates",
            "Search lean, kaizen, TPM, and continuous improvement guidance.",
            new ToolParameter("query", "The lean or improvement template search query.")),
        CreateFunctionTool(
            "identify_losses",
            "Identify top losses for a given time period and plant area.",
            new ToolParameter("period", "The period to analyze, such as last-30-days or last-quarter."),
            new ToolParameter("area", "The plant, line, or area to analyze.")),
    ];

    public override IReadOnlyList<ToolDefinition> ToolDefinitions => _toolDefinitions;

    public override async Task<string> InvokeAsync(RequiredFunctionToolCall toolCall, CancellationToken ct = default)
    {
        using JsonDocument args = JsonDocument.Parse(toolCall.Arguments);
        return toolCall.Name switch
        {
            "query_historical_data" => await QueryHistoricalDataAsync(GetRequiredString(args.RootElement, "query"), ct),
            "search_lean_templates" => await SearchLeanTemplatesAsync(GetRequiredString(args.RootElement, "query"), ct),
            "identify_losses" => await IdentifyLossesAsync(
                GetRequiredString(args.RootElement, "period"),
                GetRequiredString(args.RootElement, "area"),
                ct),
            _ => throw new InvalidOperationException($"Unsupported tool call: {toolCall.Name}"),
        };
    }

    public Task<string> QueryHistoricalDataAsync(string query, CancellationToken ct = default) =>
        fabricDataAgent.QueryAsync(query, ct);

    public async Task<string> SearchLeanTemplatesAsync(string query, CancellationToken ct = default)
    {
        var results = await knowledgeSearch.SearchAsync(query, maxResults: 4, ct: ct);
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            results.Select(r => $"**{r.Title}** (score: {r.Score:F2}){Environment.NewLine}{r.Content}"));
    }

    public Task<string> IdentifyLossesAsync(string period, string area, CancellationToken ct = default) =>
        fabricDataAgent.QueryAsync(
            $"Identify the top production losses for area {area} over {period}, including downtime, scrap, speed loss, and recurring chronic loss patterns.",
            ct);
}
