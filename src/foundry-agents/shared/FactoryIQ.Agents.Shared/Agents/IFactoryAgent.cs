namespace FactoryIQ.Agents.Shared.Agents;

public interface IFactoryAgent
{
    string Name { get; }

    Task RegisterAsync(CancellationToken ct = default);

    Task<string> RunAsync(string userQuery, CancellationToken ct = default);

    Task DeleteAsync(CancellationToken ct = default);
}
