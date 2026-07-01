using FactoryIQ.Agents.Shared.Services;

namespace FactoryIQ.Agents.PlantManager.Tools;

/// <summary>
/// Function tools exposed to the Plant Manager Agent.
/// </summary>
public sealed class PlantManagerTools(FabricDataAgentService fabricDataAgent, KnowledgeSearchService knowledgeSearch)
{
    /// <summary>
    /// Query aggregated plant KPIs from the Fabric semantic model.
    /// </summary>
    public async Task<string> QueryPlantKpisAsync(string query, CancellationToken ct = default)
    {
        return await fabricDataAgent.QueryAsync(query, ct);
    }

    /// <summary>
    /// Search escalation procedures and management playbooks (Foundry IQ / AI Search).
    /// </summary>
    public async Task<string> SearchEscalationDocsAsync(string query, CancellationToken ct = default)
    {
        var results = await knowledgeSearch.SearchAsync(query, maxResults: 3, ct: ct);
        return string.Join("\n\n", results.Select(r => $"**{r.Title}** (relevance: {r.Score:F2})\n{r.Content}"));
    }

    /// <summary>
    /// Query open work orders and action items (Work IQ).
    /// </summary>
    public async Task<string> QueryOpenItemsAsync(string plantId, CancellationToken ct = default)
    {
        return await fabricDataAgent.QueryAsync(
            $"Get all open work orders, escalations, and blocked items for plant {plantId} grouped by priority and age",
            ct);
    }

    /// <summary>
    /// Get production targets vs actual for the current period.
    /// </summary>
    public async Task<string> GetProductionTargetsAsync(string plantId, string period, CancellationToken ct = default)
    {
        return await fabricDataAgent.QueryAsync(
            $"Get production plan vs actual for plant {plantId} for period {period}, including variance percentage by product line",
            ct);
    }
}
