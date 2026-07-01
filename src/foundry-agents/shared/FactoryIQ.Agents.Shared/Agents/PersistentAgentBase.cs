using Azure.AI.Agents.Persistent;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Shared.Agents;

public abstract class PersistentAgentBase<TTools> : IPersistentFactoryAgent
    where TTools : IFunctionToolExecutor
{
    private readonly PersistentAgentsClient _client;
    private readonly AgentRunner _agentRunner;
    private readonly TTools _tools;
    private readonly FoundryConfig _config;
    private readonly ILogger _logger;
    private PersistentAgent? _registeredAgent;

    protected PersistentAgentBase(
        PersistentAgentsClient client,
        AgentRunner agentRunner,
        TTools tools,
        FoundryConfig config,
        ILogger logger)
    {
        _client = client;
        _agentRunner = agentRunner;
        _tools = tools;
        _config = config;
        _logger = logger;
    }

    public abstract string Name { get; }

    protected abstract string Description { get; }

    protected abstract string Instructions { get; }

    public async Task RegisterAsync(CancellationToken ct = default)
    {
        if (_registeredAgent is not null)
        {
            return;
        }

        _registeredAgent = await FindExistingAgentAsync(ct);
        if (_registeredAgent is not null)
        {
            _logger.LogInformation("Reusing persistent agent {AgentName} ({AgentId})", Name, _registeredAgent.Id);
            return;
        }

        _registeredAgent = await _client.Administration.CreateAgentAsync(
            model: _config.ModelDeploymentName,
            name: Name,
            description: Description,
            instructions: Instructions,
            tools: _tools.ToolDefinitions,
            toolResources: null,
            temperature: null,
            topP: null,
            responseFormat: null,
            metadata: null,
            cancellationToken: ct);

        _logger.LogInformation("Registered persistent agent {AgentName} ({AgentId})", Name, _registeredAgent.Id);
    }

    public async Task<string> RunAsync(string userQuery, CancellationToken ct = default)
    {
        await RegisterAsync(ct);
        return await _agentRunner.RunAsync(_registeredAgent!, userQuery, _tools.InvokeAsync, ct);
    }

    public async Task DeleteAsync(CancellationToken ct = default)
    {
        if (_registeredAgent is null)
        {
            return;
        }

        await _client.Administration.DeleteAgentAsync(_registeredAgent.Id, ct);
        _logger.LogInformation("Deleted persistent agent {AgentName} ({AgentId})", Name, _registeredAgent.Id);
        _registeredAgent = null;
    }

    private async Task<PersistentAgent?> FindExistingAgentAsync(CancellationToken ct)
    {
        await foreach (PersistentAgent candidate in _client.Administration.GetAgentsAsync(
            limit: 100,
            order: null,
            after: null,
            before: null,
            cancellationToken: ct))
        {
            if (string.Equals(candidate.Name, Name, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }
}
