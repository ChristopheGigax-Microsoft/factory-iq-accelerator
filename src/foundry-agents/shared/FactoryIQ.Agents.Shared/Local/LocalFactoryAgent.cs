using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Shared.Local;

public sealed class LocalFactoryAgent(
    FactoryAgentProfile profile,
    LocalModelRuntime modelRuntime,
    ILogger<LocalFactoryAgent> logger) : IFactoryAgent
{
    public string Name => profile.Name;

    public bool IsLocal => true;

    public Task RegisterAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Preparing local Factory IQ agent {AgentName}.", Name);
        return modelRuntime.EnsureReadyAsync(ct);
    }

    public Task<string> RunAsync(string userQuery, CancellationToken ct = default)
    {
        string prompt = $"{profile.LocalInstructions}\n\nUser request:\n{userQuery}";
        return modelRuntime.CompleteAsync(prompt, ct);
    }

    public Task DeleteAsync(CancellationToken ct = default) =>
        modelRuntime.UnloadAsync();
}
