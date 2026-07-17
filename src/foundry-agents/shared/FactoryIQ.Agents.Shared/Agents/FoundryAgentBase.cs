using Azure;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;
using System.ClientModel;

namespace FactoryIQ.Agents.Shared.Agents;

public abstract class FoundryAgentBase : IFactoryAgent
{
    private const string KnowledgeBaseToolLabel = "knowledge-base";
    private const string KnowledgeBaseRetrieveToolName = "knowledge_base_retrieve";
    private const string KnowledgeBaseMcpApiVersion = "2026-05-01-preview";

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

    protected virtual bool UsesFoundryIqKnowledgeBase => true;
    protected virtual bool UsesFabricDataAgentTool => true;
    protected virtual bool UsesWorkIqTool => false;

    public async Task RegisterAsync(CancellationToken ct = default)
    {
        if (_registeredAgent is not null)
        {
            return;
        }

        string? fabricDataAgentProjectConnectionId = null;
        if (UsesFabricDataAgentTool)
        {
            fabricDataAgentProjectConnectionId = await ResolveProjectConnectionIdAsync(
                _config.FabricDataAgentProjectConnectionName,
                ct);
        }

        string? workIqProjectConnectionId = null;
        if (UsesWorkIqTool && !string.IsNullOrWhiteSpace(_config.WorkIqProjectConnectionName))
        {
            workIqProjectConnectionId = await ResolveProjectConnectionIdAsync(
                _config.WorkIqProjectConnectionName,
                ct);
        }

        ProjectsAgentRecord? existingAgent = await FindExistingAgentAsync(ct);
        if (existingAgent is not null)
        {
            ProjectsAgentVersion latestVersion = existingAgent.GetLatestVersion();
            if (HasDesiredDefinition(latestVersion, fabricDataAgentProjectConnectionId, workIqProjectConnectionId))
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
            options: BuildAgentVersionOptions(fabricDataAgentProjectConnectionId, workIqProjectConnectionId),
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

    private ProjectsAgentVersionCreationOptions BuildAgentVersionOptions(
        string? fabricDataAgentProjectConnectionId,
        string? workIqProjectConnectionId)
    {
        DeclarativeAgentDefinition definition = new(model: _config.ModelDeploymentName)
        {
            Instructions = Instructions,
        };

        if (UsesFoundryIqKnowledgeBase)
        {
            definition.Tools.Add(BuildKnowledgeBaseTool());
        }
        if (UsesFabricDataAgentTool)
        {
            definition.Tools.Add(BuildFabricDataAgentTool(fabricDataAgentProjectConnectionId));
        }
        if (UsesWorkIqTool && workIqProjectConnectionId is not null)
        {
            definition.Tools.Add(BuildWorkIqTool(workIqProjectConnectionId));
        }

        return new ProjectsAgentVersionCreationOptions(definition)
        {
            Description = Description,
        };
    }

    private bool HasDesiredDefinition(
        ProjectsAgentVersion agentVersion,
        string? fabricDataAgentProjectConnectionId,
        string? workIqProjectConnectionId)
    {
        if (agentVersion.Definition is not DeclarativeAgentDefinition definition)
        {
            return false;
        }

        return string.Equals(agentVersion.Description, Description, StringComparison.Ordinal)
            && string.Equals(definition.Model, _config.ModelDeploymentName, StringComparison.Ordinal)
            && string.Equals(definition.Instructions, Instructions, StringComparison.Ordinal)
            && HasExpectedKnowledgeBaseTool(definition)
            && HasExpectedFabricDataAgentTool(definition, fabricDataAgentProjectConnectionId)
            && HasExpectedWorkIqTool(definition, workIqProjectConnectionId);
    }

    private bool HasExpectedKnowledgeBaseTool(DeclarativeAgentDefinition definition)
    {
        if (!UsesFoundryIqKnowledgeBase)
        {
            return true;
        }

        McpTool? kbTool = definition.Tools
            .OfType<McpTool>()
            .FirstOrDefault(tool => string.Equals(tool.ServerLabel, KnowledgeBaseToolLabel, StringComparison.Ordinal));
        if (kbTool is null)
        {
            return false;
        }

        if (!Uri.TryCreate(kbTool.ServerUri?.ToString(), UriKind.Absolute, out Uri? existingServerUri)
            || !Uri.Equals(existingServerUri, BuildKnowledgeBaseMcpUri()))
        {
            return false;
        }

        if (kbTool.AllowedTools?.ToolNames is null
            || !kbTool.AllowedTools.ToolNames.Contains(KnowledgeBaseRetrieveToolName, StringComparer.Ordinal))
        {
            return false;
        }

        // URI and allowed tools are sufficient to detect version drift.
        // project_connection_id is set at creation time via JsonPatch but cannot be read back (write-only path).
        return true;
    }

    private McpTool BuildKnowledgeBaseTool()
    {
        McpTool tool = ResponseTool.CreateMcpTool(
            serverLabel: KnowledgeBaseToolLabel,
            serverUri: BuildKnowledgeBaseMcpUri(),
            toolCallApprovalPolicy: GlobalMcpToolCallApprovalPolicy.NeverRequireApproval);

        tool.AllowedTools = new McpToolFilter();
        tool.AllowedTools.ToolNames.Add(KnowledgeBaseRetrieveToolName);

        tool.Patch.Set("$.project_connection_id"u8, _config.KnowledgeBaseProjectConnectionName);

        return tool;
    }

    private bool HasExpectedFabricDataAgentTool(DeclarativeAgentDefinition definition, string? fabricDataAgentProjectConnectionId)
    {
        if (!UsesFabricDataAgentTool)
        {
            return true;
        }

        string expectedConnection = fabricDataAgentProjectConnectionId ?? _config.FabricDataAgentProjectConnectionName;
        return definition.Tools
            .Any(tool => tool.ToString()?.Contains(expectedConnection, StringComparison.Ordinal) == true);
    }

    private ResponseTool BuildFabricDataAgentTool(string? fabricDataAgentProjectConnectionId)
    {
        string projectConnectionId = fabricDataAgentProjectConnectionId ?? _config.FabricDataAgentProjectConnectionName;
        return new FabricIQPreviewTool(projectConnectionId)
        {
            RequireApproval = GlobalMcpToolCallApprovalPolicy.NeverRequireApproval,
        };
    }

    private bool HasExpectedWorkIqTool(DeclarativeAgentDefinition definition, string? workIqProjectConnectionId)
    {
        if (!UsesWorkIqTool || string.IsNullOrWhiteSpace(_config.WorkIqProjectConnectionName))
        {
            return true;
        }

        string expectedConnection = workIqProjectConnectionId ?? _config.WorkIqProjectConnectionName;
        return definition.Tools
            .Any(tool => tool.ToString()?.Contains(expectedConnection, StringComparison.Ordinal) == true);
    }

    private ResponseTool BuildWorkIqTool(string workIqProjectConnectionId)
    {
        return new WorkIQPreviewTool(workIqProjectConnectionId);
    }

    private async Task<string> ResolveProjectConnectionIdAsync(string connectionNameOrId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(connectionNameOrId))
        {
            return connectionNameOrId;
        }

        if (connectionNameOrId.StartsWith("/subscriptions/", StringComparison.OrdinalIgnoreCase))
        {
            return connectionNameOrId;
        }

        try
        {
            ClientResult<AIProjectConnection> result = await _projectClient.Connections.GetConnectionAsync(
                connectionNameOrId,
                includeCredentials: false,
                cancellationToken: ct);
            return string.IsNullOrWhiteSpace(result.Value.Id) ? connectionNameOrId : result.Value.Id;
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            _logger.LogWarning(
                "Fabric project connection {ConnectionName} not found by name. Using configured value as-is.",
                connectionNameOrId);
            return connectionNameOrId;
        }
    }

    private Uri BuildKnowledgeBaseMcpUri()
    {
        return new Uri($"{_config.SearchEndpoint.TrimEnd('/')}/knowledgebases/{_config.KnowledgeBaseName}/mcp?api-version={KnowledgeBaseMcpApiVersion}");
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
