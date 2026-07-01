using FactoryIQ.Agents.Shared.Services;

namespace FactoryIQ.Agents.Maintenance.Tools;

/// <summary>
/// Function tools exposed to the Maintenance Agent.
/// </summary>
public sealed class MaintenanceTools(FabricDataAgentService fabricDataAgent, KnowledgeSearchService knowledgeSearch)
{
    /// <summary>
    /// Query sensor data and alarms from the KQL database.
    /// </summary>
    public async Task<string> QuerySensorDataAsync(string query, CancellationToken ct = default)
    {
        return await fabricDataAgent.QueryAsync(query, ct);
    }

    /// <summary>
    /// Search maintenance procedures, runbooks, and OEM manuals (Foundry IQ / AI Search).
    /// </summary>
    public async Task<string> SearchMaintenanceDocsAsync(string query, CancellationToken ct = default)
    {
        var results = await knowledgeSearch.SearchAsync(query, maxResults: 4, ct: ct);
        return string.Join("\n\n", results.Select(r => $"**{r.Title}** (relevance: {r.Score:F2})\n{r.Content}"));
    }

    /// <summary>
    /// Query work orders from Work IQ (structured task/work order data).
    /// </summary>
    public async Task<string> QueryWorkOrdersAsync(string machineId, string timeRange, CancellationToken ct = default)
    {
        return await fabricDataAgent.QueryAsync(
            $"Get all work orders for machine {machineId} in the last {timeRange}, including status, type, and completion details",
            ct);
    }

    /// <summary>
    /// Get full asset maintenance history.
    /// </summary>
    public async Task<string> GetAssetHistoryAsync(string machineId, CancellationToken ct = default)
    {
        return await fabricDataAgent.QueryAsync(
            $"Get complete maintenance history for asset {machineId} including preventive, corrective, and emergency work orders with outcomes",
            ct);
    }
}
