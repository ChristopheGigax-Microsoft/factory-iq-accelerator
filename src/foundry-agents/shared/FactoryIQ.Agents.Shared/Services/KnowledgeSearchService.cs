using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Shared.Services;

/// <summary>
/// Provides RAG-style document retrieval over the Foundry IQ knowledge base (Azure AI Search).
/// </summary>
public sealed class KnowledgeSearchService(SearchClient searchClient, ILogger<KnowledgeSearchService> logger)
{
    /// <summary>
    /// Searches the knowledge base using semantic/hybrid search and returns relevant document chunks.
    /// </summary>
    public async Task<IReadOnlyList<KnowledgeResult>> SearchAsync(string query, int maxResults = 5, CancellationToken ct = default)
    {
        logger.LogInformation("Searching knowledge base: '{Query}'", query);

        var options = new SearchOptions
        {
            Size = maxResults,
            QueryType = SearchQueryType.Semantic,
            SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = "default",
            },
        };

        var response = await searchClient.SearchAsync<SearchDocument>(query, options, ct);
        var results = new List<KnowledgeResult>();

        await foreach (var result in response.Value.GetResultsAsync())
        {
            var content = result.Document.GetString("content") ?? "";
            var title = result.Document.GetString("title") ?? "Untitled";
            var source = result.Document.GetString("source") ?? "";

            results.Add(new KnowledgeResult
            {
                Title = title,
                Content = content,
                Source = source,
                Score = result.Score ?? 0,
            });
        }

        logger.LogInformation("Found {Count} results for query", results.Count);
        return results;
    }
}

public sealed record KnowledgeResult
{
    public required string Title { get; init; }
    public required string Content { get; init; }
    public required string Source { get; init; }
    public double Score { get; init; }
}
