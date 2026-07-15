using Azure.AI.Agents.Persistent;
using FactoryIQ.Agents.Shared.Agents;

namespace FactoryIQ.Agents.Operations.Tools;

/// <summary>
/// Operations agent tools — connectors will be added incrementally.
/// </summary>
public sealed class OperationsTools : FunctionToolExecutorBase
{
    public override IReadOnlyList<ToolDefinition> ToolDefinitions => [];

    public override Task<string> InvokeAsync(RequiredFunctionToolCall toolCall, CancellationToken ct = default) =>
        throw new InvalidOperationException($"No tools configured yet. Received: {toolCall.Name}");
}
