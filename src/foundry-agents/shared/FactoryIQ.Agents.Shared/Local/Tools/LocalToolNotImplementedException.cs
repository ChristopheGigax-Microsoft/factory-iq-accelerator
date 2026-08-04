namespace FactoryIQ.Agents.Shared.Local.Tools;

public sealed class LocalToolNotImplementedException(string toolName)
    : NotSupportedException(
        $"The local tool '{toolName}' has no client implementation. Configure it for the target plant before using it.");
