using FactoryIQ.Agents.Shared.Services;

namespace FactoryIQ.Agents.Operations.Tools;

/// <summary>
/// Function tools exposed to the Operations Agent for querying plant telemetry.
/// </summary>
public sealed class OperationsTools(FabricDataAgentService fabricDataAgent, KnowledgeSearchService knowledgeSearch)
{
    /// <summary>
    /// Query real-time machine telemetry from the KQL database via the Fabric Data Agent.
    /// </summary>
    public async Task<string> QueryTelemetryAsync(string query, CancellationToken ct = default)
    {
        return await fabricDataAgent.QueryAsync(query, ct);
    }

    /// <summary>
    /// Search the OEE procedures and deviation playbooks in the knowledge base.
    /// </summary>
    public async Task<string> SearchKnowledgeAsync(string query, CancellationToken ct = default)
    {
        var results = await knowledgeSearch.SearchAsync(query, maxResults: 3, ct: ct);
        return string.Join("\n\n", results.Select(r => $"**{r.Title}** (score: {r.Score:F2})\n{r.Content}"));
    }

    /// <summary>
    /// Get current OEE metrics for a specific work center or plant.
    /// </summary>
    public async Task<string> GetOeeMetricsAsync(string entityId, CancellationToken ct = default)
    {
        return await fabricDataAgent.QueryAsync(
            $"Calculate the current OEE (Availability, Performance, Quality) for entity {entityId} over the last shift",
            ct);
    }
}
