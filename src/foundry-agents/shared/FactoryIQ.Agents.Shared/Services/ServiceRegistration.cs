using Azure.AI.Projects;
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
    public static IServiceCollection AddFoundryAgentServices(this IServiceCollection services, FoundryConfig config)
    {
        services.AddSingleton(config);

        services.AddSingleton(_ => new AIProjectClient(
            new Uri(config.ProjectEndpoint),
            new DefaultAzureCredential()));

        services.AddSingleton(_ => new SearchClient(
            new Uri(config.AiSearchEndpoint),
            "knowledge-base",
            new DefaultAzureCredential()));

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
            ProjectEndpoint = GetRequired("AZURE_AI_PROJECT_ENDPOINT"),
            ModelDeploymentName = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME") ?? "gpt-4o",
            AiSearchEndpoint = GetRequired("AI_SEARCH_ENDPOINT"),
            StorageAccountEndpoint = GetRequired("STORAGE_ACCOUNT_ENDPOINT"),
            DataAgentId = Environment.GetEnvironmentVariable("FABRIC_DATA_AGENT_ID"),
            WorkspaceId = Environment.GetEnvironmentVariable("FABRIC_WORKSPACE_ID"),
        };
    }

    private static string GetRequired(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Missing required environment variable: {name}");
}
