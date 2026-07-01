using Azure.AI.Agents.Persistent;
using FactoryIQ.Agents.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Shared.Services;

/// <summary>
/// Wraps the Fabric Data Agent for querying KQL telemetry data.
/// </summary>
public sealed class FabricDataAgentService(
    PersistentAgentsClient persistentAgentsClient,
    AgentRunner agentRunner,
    FoundryConfig config,
    ILogger<FabricDataAgentService> logger)
{
    private PersistentAgent? _cachedAgent;

    public async Task<string> QueryAsync(string naturalLanguageQuery, CancellationToken ct = default)
    {
        logger.LogInformation("Querying Fabric Data Agent: '{Query}'", naturalLanguageQuery);

        if (string.IsNullOrWhiteSpace(config.DataAgentId))
        {
            logger.LogWarning("Fabric Data Agent ID not configured; returning empty result");
            return "Data Agent not configured. Please set FABRIC_DATA_AGENT_ID.";
        }

        try
        {
            _cachedAgent ??= await persistentAgentsClient.Administration.GetAgentAsync(config.DataAgentId, ct);
            return await agentRunner.RunAsync(_cachedAgent, naturalLanguageQuery, UnsupportedToolAsync, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fabric Data Agent query failed");
            return $"Error querying Data Agent: {ex.Message}";
        }
    }

    private static Task<string> UnsupportedToolAsync(RequiredFunctionToolCall toolCall, CancellationToken ct)
    {
        return Task.FromException<string>(
            new InvalidOperationException($"Fabric Data Agent requested unsupported tool '{toolCall.Name}'."));
    }
}
