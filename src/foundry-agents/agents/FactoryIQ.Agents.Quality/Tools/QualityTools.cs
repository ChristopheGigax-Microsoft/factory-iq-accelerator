using FactoryIQ.Agents.Shared.Services;

namespace FactoryIQ.Agents.Quality.Tools;

/// <summary>
/// Function tools exposed to the Quality Agent.
/// </summary>
public sealed class QualityTools(FabricDataAgentService fabricDataAgent, KnowledgeSearchService knowledgeSearch)
{
    /// <summary>
    /// Query quality inspection data, scrap records, and SPC metrics from KQL.
    /// </summary>
    public async Task<string> QueryQualityDataAsync(string query, CancellationToken ct = default)
    {
        return await fabricDataAgent.QueryAsync(query, ct);
    }

    /// <summary>
    /// Search quality standards, SPC documentation, and inspection criteria (Foundry IQ / AI Search).
    /// </summary>
    public async Task<string> SearchQualityStandardsAsync(string query, CancellationToken ct = default)
    {
        var results = await knowledgeSearch.SearchAsync(query, maxResults: 4, ct: ct);
        return string.Join("\n\n", results.Select(r => $"**{r.Title}** (relevance: {r.Score:F2})\n{r.Content}"));
    }

    /// <summary>
    /// Search external supplier specifications and industry benchmarks (Web IQ).
    /// In production, this would call a web search API or supplier portal.
    /// </summary>
    public Task<string> SearchWebAsync(string query, CancellationToken ct = default)
    {
        // Placeholder: In production, integrate with Bing/Web IQ for external supplier spec lookups
        return Task.FromResult($"[Web IQ] Search results for: {query}\n(Web IQ integration pending configuration)");
    }

    /// <summary>
    /// Get process parameter history for a specific batch and machine.
    /// </summary>
    public async Task<string> GetProcessParametersAsync(string batchId, string machineId, CancellationToken ct = default)
    {
        return await fabricDataAgent.QueryAsync(
            $"Get all process parameters (temperature, pressure, speed, humidity) for batch {batchId} on machine {machineId} with timestamps",
            ct);
    }
}
