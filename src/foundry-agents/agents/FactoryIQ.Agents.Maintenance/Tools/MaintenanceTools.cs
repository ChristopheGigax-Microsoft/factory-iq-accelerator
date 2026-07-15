using Azure.AI.Agents.Persistent;
using FactoryIQ.Agents.Shared.Agents;

namespace FactoryIQ.Agents.Maintenance.Tools;

/// <summary>
/// Maintenance agent tools — connectors will be added incrementally.
/// </summary>
public sealed class MaintenanceTools : FunctionToolExecutorBase
{
    public override IReadOnlyList<ToolDefinition> ToolDefinitions => [];

    public override Task<string> InvokeAsync(RequiredFunctionToolCall toolCall, CancellationToken ct = default) =>
        throw new InvalidOperationException($"No tools configured yet. Received: {toolCall.Name}");
}
