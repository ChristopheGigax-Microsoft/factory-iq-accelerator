using Azure.AI.Projects;
using Azure.Identity;
using FactoryIQ.Agents.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Shared.Services;

/// <summary>
/// Registers shared services into the DI container for all agents.
/// </summary>
public static class ServiceRegistration
{
    private const string DefaultProjectEndpoint = "https://fiq-plant1-dev-ai-foundry.services.ai.azure.com/api/projects/fiq-plant1-dev-ai-project";
    private const string DefaultSearchEndpoint = "https://fiq-plant1-dev-search.search.windows.net";
    private const string DefaultKnowledgeBaseName = "fiq-plant1-dev-kb";
    private const string DefaultKnowledgeBaseProjectConnectionName = "foundry-iq-kb-connection";
    private const string DefaultFabricDataAgentProjectConnectionName = "fabric-iq-data-agent-connection";

    public static IServiceCollection AddFoundryAgentServices(this IServiceCollection services, FoundryConfig config)
    {
        services.AddSingleton(config);
        bool useManagedIdentity = bool.TryParse(
            Environment.GetEnvironmentVariable("USE_MANAGED_IDENTITY"),
            out bool managedIdentityEnabled)
            && managedIdentityEnabled;

        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            TenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID"),
            ExcludeVisualStudioCredential = true,
            ExcludeManagedIdentityCredential = !useManagedIdentity,
        });

        services.AddSingleton(_ => new AIProjectClient(
            new Uri(config.ProjectEndpoint),
            credential));

        services.AddSingleton<AgentRunner>();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSimpleConsole(o =>
            {
                o.SingleLine = true;
                o.TimestampFormat = "HH:mm:ss ";
            });
            builder.SetMinimumLevel(LogLevel.Information);
        });

        return services;
    }

    public static FoundryConfig LoadConfigFromEnvironment()
    {
        return new FoundryConfig
        {
            ProjectEndpoint = Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")
                ?? Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
                ?? DefaultProjectEndpoint,
            ModelDeploymentName = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME") ?? "gpt-4o",
            SearchEndpoint = Environment.GetEnvironmentVariable("AI_SEARCH_ENDPOINT")
                ?? DefaultSearchEndpoint,
            KnowledgeBaseName = Environment.GetEnvironmentVariable("FOUNDRY_IQ_KNOWLEDGE_BASE_NAME")
                ?? DefaultKnowledgeBaseName,
            KnowledgeBaseProjectConnectionName = Environment.GetEnvironmentVariable("FOUNDRY_IQ_PROJECT_CONNECTION_NAME")
                ?? DefaultKnowledgeBaseProjectConnectionName,
            FabricDataAgentProjectConnectionName = Environment.GetEnvironmentVariable("FOUNDRY_FABRIC_DATA_AGENT_PROJECT_CONNECTION_NAME")
                ?? DefaultFabricDataAgentProjectConnectionName,
            WorkIqProjectConnectionName = Environment.GetEnvironmentVariable("FOUNDRY_WORK_IQ_PROJECT_CONNECTION_NAME")
                ?? string.Empty,
            DeletePersistentAgentOnExit = bool.TryParse(
                Environment.GetEnvironmentVariable("DELETE_PERSISTENT_AGENT_ON_EXIT"),
                out var deleteOnExit)
                && deleteOnExit, // default: false — agents stay persistent in Foundry
        };
    }
}
