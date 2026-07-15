using Azure;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.Logging;
using System.ClientModel;

namespace FactoryIQ.Agents.Shared.Agents;

public abstract class FoundryAgentBase : IFactoryAgent
{
    private readonly AIProjectClient _projectClient;
    private readonly AgentRunner _agentRunner;
    private readonly FoundryConfig _config;
    private readonly ILogger _logger;
    private FoundryAgent? _registeredAgent;
    private ProjectsAgentVersion? _registeredVersion;

    protected FoundryAgentBase(
        AIProjectClient projectClient,
        AgentRunner agentRunner,
        FoundryConfig config,
        ILogger logger)
    {
        _projectClient = projectClient;
        _agentRunner = agentRunner;
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

        ProjectsAgentRecord? existingAgent = await FindExistingAgentAsync(ct);
        if (existingAgent is not null)
        {
            ProjectsAgentVersion latestVersion = existingAgent.GetLatestVersion();
            if (HasDesiredDefinition(latestVersion))
            {
                _registeredVersion = latestVersion;
                _registeredAgent = _projectClient.AsAIAgent(existingAgent);
                _logger.LogInformation(
                    "Reusing Foundry agent {AgentName} ({AgentId}) version {AgentVersion}",
                    Name,
                    latestVersion.Id,
                    latestVersion.Version);
                return;
            }
        }

        ClientResult<ProjectsAgentVersion> createdVersion = await _projectClient.AgentAdministrationClient.CreateAgentVersionAsync(
            agentName: Name,
            options: BuildAgentVersionOptions(),
            cancellationToken: ct);

        _registeredVersion = createdVersion.Value;
        _registeredAgent = _projectClient.AsAIAgent(_registeredVersion);

        _logger.LogInformation(
            existingAgent is null
                ? "Registered Foundry agent {AgentName} ({AgentId}) version {AgentVersion}"
                : "Published new version for Foundry agent {AgentName} ({AgentId}) version {AgentVersion}",
            Name,
            _registeredVersion.Id,
            _registeredVersion.Version);
    }

    public async Task<string> RunAsync(string userQuery, CancellationToken ct = default)
    {
        await RegisterAsync(ct);
        return await _agentRunner.RunAsync(_registeredAgent!, userQuery, ct);
    }

    public async Task DeleteAsync(CancellationToken ct = default)
    {
        if (_registeredVersion is null)
        {
            return;
        }

        await _projectClient.AgentAdministrationClient.DeleteAgentAsync(Name, ct);
        _logger.LogInformation("Deleted Foundry agent {AgentName}", Name);
        _registeredAgent = null;
        _registeredVersion = null;
    }

    private ProjectsAgentVersionCreationOptions BuildAgentVersionOptions()
    {
        DeclarativeAgentDefinition definition = new(model: _config.ModelDeploymentName)
        {
            Instructions = Instructions,
        };

        return new ProjectsAgentVersionCreationOptions(definition)
        {
            Description = Description,
        };
    }

    private bool HasDesiredDefinition(ProjectsAgentVersion agentVersion)
    {
        if (agentVersion.Definition is not DeclarativeAgentDefinition definition)
        {
            return false;
        }

        return string.Equals(agentVersion.Description, Description, StringComparison.Ordinal)
            && string.Equals(definition.Model, _config.ModelDeploymentName, StringComparison.Ordinal)
            && string.Equals(definition.Instructions, Instructions, StringComparison.Ordinal);
    }

    private async Task<ProjectsAgentRecord?> FindExistingAgentAsync(CancellationToken ct)
    {
        try
        {
            ClientResult<ProjectsAgentRecord> result = await _projectClient.AgentAdministrationClient.GetAgentAsync(Name, ct);
            return result.Value;
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}
