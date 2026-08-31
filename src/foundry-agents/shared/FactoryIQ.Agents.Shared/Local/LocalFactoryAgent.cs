using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Local.Tools.OpcUa;
using FactoryIQ.Agents.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Shared.Local;

public sealed class LocalFactoryAgent(
    FactoryAgentProfile profile,
    LocalModelRuntime modelRuntime,
    ILogger<LocalFactoryAgent> logger,
    OpcUaMachineDataTool? opcUaMachineDataTool = null) : IFactoryAgent
{
    public string Name => profile.Name;

    public bool IsLocal => true;

    public Task RegisterAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Preparing local Factory IQ agent {AgentName}.", Name);
        return modelRuntime.EnsureReadyAsync(ct);
    }

    public async Task<string> RunAsync(string userQuery, CancellationToken ct = default)
    {
        string opcUaContext = opcUaMachineDataTool is null
            ? "Local OPC UA live context is unavailable: OPC UA tool is not configured."
            : await opcUaMachineDataTool.BuildFactorySnapshotAsync(userQuery, ct);

        string prompt = $"{profile.LocalInstructions}\n\n{opcUaContext}\n\nUser request:\n{userQuery}";
        return await modelRuntime.CompleteAsync(prompt, ct);
    }

    public Task DeleteAsync(CancellationToken ct = default) =>
        modelRuntime.UnloadAsync();
}
