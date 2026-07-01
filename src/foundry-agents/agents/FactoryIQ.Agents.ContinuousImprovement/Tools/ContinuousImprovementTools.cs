using FactoryIQ.Agents.Shared.Services;

namespace FactoryIQ.Agents.ContinuousImprovement.Tools;

/// <summary>
/// Function tools exposed to the Continuous Improvement Agent.
/// </summary>
public sealed class ContinuousImprovementTools(FabricDataAgentService fabricDataAgent, KnowledgeSearchService knowledgeSearch)
{
    /// <summary>
    /// Query historical production, downtime, and loss data from KQL.
    /// </summary>
    public async Task<string> QueryHistoricalDataAsync(string query, CancellationToken ct = default)
    {
        return await fabricDataAgent.QueryAsync(query, ct);
    }

    /// <summary>
    /// Search lean/kaizen templates, TPM guides, and improvement frameworks (Foundry IQ / AI Search).
    /// </summary>
    public async Task<string> SearchLeanTemplatesAsync(string query, CancellationToken ct = default)
    {
        var results = await knowledgeSearch.SearchAsync(query, maxResults: 4, ct: ct);
        return string.Join("\n\n", results.Select(r => $"**{r.Title}** (relevance: {r.Score:F2})\n{r.Content}"));
    }

    /// <summary>
    /// Search industry benchmarks and best practices (Web IQ).
    /// In production, this integrates with external search for manufacturing benchmarks.
    /// </summary>
    public Task<string> SearchBenchmarksAsync(string query, CancellationToken ct = default)
    {
        // Placeholder: In production, integrate with Bing/Web IQ for industry benchmark lookups
        return Task.FromResult($"[Web IQ] Benchmark search for: {query}\n(Web IQ integration pending configuration)");
    }

    /// <summary>
    /// Get Pareto analysis of losses by category for a plant.
    /// </summary>
    public async Task<string> GetLossParetoAsync(string plantId, string period, CancellationToken ct = default)
    {
        return await fabricDataAgent.QueryAsync(
            $"Generate Pareto chart data for production losses at plant {plantId} over {period}: rank by lost hours, show cumulative percentage, categorize by six big losses",
            ct);
    }
}
