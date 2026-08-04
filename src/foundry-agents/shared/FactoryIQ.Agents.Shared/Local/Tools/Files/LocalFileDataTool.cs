using FactoryIQ.Agents.Shared.Local.Tools.Contracts;

namespace FactoryIQ.Agents.Shared.Local.Tools.Files;

public sealed class LocalFileDataTool : ILocalFileOperations
{
    public Task<IReadOnlyList<LocalDocument>> SearchDocumentsAsync(
        string query,
        CancellationToken ct = default) =>
        throw new LocalToolNotImplementedException(nameof(LocalFileDataTool));
}
