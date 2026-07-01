using Azure.AI.Projects;
using FactoryIQ.Agents.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Shared.Services;

/// <summary>
/// Wraps the Fabric Data Agent SDK for querying KQL telemetry data.
/// Agents use this to query real-time and historical time-series data from the Eventhouse.
/// </summary>
public sealed class FabricDataAgentService(AIProjectClient projectClient, FoundryConfig config, ILogger<FabricDataAgentService> logger)
{
    /// <summary>
    /// Sends a natural-language query to the Fabric Data Agent and returns the response text.
    /// The Data Agent translates it into KQL and executes against the Eventhouse.
    /// </summary>
    public async Task<string> QueryAsync(string naturalLanguageQuery, CancellationToken ct = default)
    {
        logger.LogInformation("Querying Fabric Data Agent: '{Query}'", naturalLanguageQuery);

        if (string.IsNullOrEmpty(config.DataAgentId))
        {
            logger.LogWarning("Fabric Data Agent ID not configured; returning empty result");
            return "Data Agent not configured. Please set FABRIC_DATA_AGENT_ID.";
        }

        try
        {
            var agent = projectClient.GetAIAgent(name: "FabricDataAgent", cancellationToken: ct);
            var response = await agent.RunAsync(naturalLanguageQuery, thread: null, options: null, cancellationToken: ct);
            return response.Text ?? "No response from Data Agent.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fabric Data Agent query failed");
            return $"Error querying Data Agent: {ex.Message}";
        }
    }
}
