using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Shared.Services;

/// <summary>
/// Runs a Foundry Agent Service session and returns the textual result.
/// </summary>
public sealed class AgentRunner(ILogger<AgentRunner> logger)
{
    public async Task<string> RunAsync(FoundryAgent agent, string userQuery, CancellationToken ct = default)
    {
        AgentSession session = await agent.CreateSessionAsync();
        logger.LogInformation("Running Foundry agent {AgentName}", agent.Name);
        AgentResponse response = await agent.RunAsync(userQuery, session);
        return response.Text;
    }
}
