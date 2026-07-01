using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Azure.Search.Documents;
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

    public static IServiceCollection AddFoundryAgentServices(this IServiceCollection services, FoundryConfig config)
    {
        services.AddSingleton(config);

        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            TenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID"),
            ExcludeVisualStudioCredential = true,
        });

        services.AddSingleton(_ => new PersistentAgentsClient(
            config.ProjectEndpoint,
            credential));

        services.AddSingleton(_ => new SearchClient(
            new Uri(config.AiSearchEndpoint),
            "knowledge-base",
            new DefaultAzureCredential()));

        services.AddSingleton<AgentRunner>();
        services.AddSingleton<KnowledgeSearchService>();
        services.AddSingleton<FabricDataAgentService>();

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
            AiSearchEndpoint = GetRequired("AI_SEARCH_ENDPOINT"),
            StorageAccountEndpoint = Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_ENDPOINT"),
            DataAgentId = Environment.GetEnvironmentVariable("FABRIC_DATA_AGENT_ID"),
            WorkspaceId = Environment.GetEnvironmentVariable("FABRIC_WORKSPACE_ID"),
            DeletePersistentAgentOnExit = bool.TryParse(
                Environment.GetEnvironmentVariable("DELETE_PERSISTENT_AGENT_ON_EXIT"),
                out var deleteOnExit)
                && deleteOnExit,
        };
    }

    private static string GetRequired(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Missing required environment variable: {name}");
}
