using FactoryIQ.Agents.ContinuousImprovement;
using FactoryIQ.Agents.Maintenance;
using FactoryIQ.Agents.Operations;
using FactoryIQ.Agents.PlantManager;
using FactoryIQ.Agents.Quality;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Local;
using FactoryIQ.Agents.Shared.Local.Tools.OpcUa;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5080");

var config = ServiceRegistration.LoadConfigFromEnvironment();

if (config.Runtime == AgentRuntime.Local)
{
    builder.Services.AddLocalAgentServices(config);
}
else
{
    builder.Services.AddFoundryAgentServices(config);
    builder.Services.AddSingleton<OperationsAgent>();
    builder.Services.AddSingleton<MaintenanceAgent>();
    builder.Services.AddSingleton<QualityAgent>();
    builder.Services.AddSingleton<PlantManagerAgent>();
    builder.Services.AddSingleton<ContinuousImprovementAgent>();
}

builder.Services.AddCors(options =>
{
    // Demo-only: allow the static HTML portal (served from file:// or any localhost port) to call this API.
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();
app.UseCors();

// Build the five IFactoryAgent instances (cloud FoundryAgentBase-derived, or LocalFactoryAgent).
Dictionary<string, IFactoryAgent> agents = config.Runtime == AgentRuntime.Local
    ? new()
    {
        ["operations"] = new LocalFactoryAgent(
            FactoryAgentProfiles.Operations,
            app.Services.GetRequiredService<LocalModelRuntime>(),
            app.Services.GetRequiredService<ILogger<LocalFactoryAgent>>(),
            app.Services.GetRequiredService<OpcUaMachineDataTool>()),
        ["maintenance"] = new LocalFactoryAgent(
            FactoryAgentProfiles.Maintenance,
            app.Services.GetRequiredService<LocalModelRuntime>(),
            app.Services.GetRequiredService<ILogger<LocalFactoryAgent>>(),
            app.Services.GetRequiredService<OpcUaMachineDataTool>()),
        ["quality"] = new LocalFactoryAgent(
            FactoryAgentProfiles.Quality,
            app.Services.GetRequiredService<LocalModelRuntime>(),
            app.Services.GetRequiredService<ILogger<LocalFactoryAgent>>(),
            app.Services.GetRequiredService<OpcUaMachineDataTool>()),
        ["plant-manager"] = new LocalFactoryAgent(
            FactoryAgentProfiles.PlantManager,
            app.Services.GetRequiredService<LocalModelRuntime>(),
            app.Services.GetRequiredService<ILogger<LocalFactoryAgent>>(),
            app.Services.GetRequiredService<OpcUaMachineDataTool>()),
        ["continuous-improvement"] = new LocalFactoryAgent(
            FactoryAgentProfiles.ContinuousImprovement,
            app.Services.GetRequiredService<LocalModelRuntime>(),
            app.Services.GetRequiredService<ILogger<LocalFactoryAgent>>(),
            app.Services.GetRequiredService<OpcUaMachineDataTool>()),
    }
    : new()
    {
        ["operations"] = app.Services.GetRequiredService<OperationsAgent>(),
        ["maintenance"] = app.Services.GetRequiredService<MaintenanceAgent>(),
        ["quality"] = app.Services.GetRequiredService<QualityAgent>(),
        ["plant-manager"] = app.Services.GetRequiredService<PlantManagerAgent>(),
        ["continuous-improvement"] = app.Services.GetRequiredService<ContinuousImprovementAgent>(),
    };

// Portal metadata shown as cards in the HTML UI.
var agentDisplay = new Dictionary<string, (string DisplayName, string Icon, string Description)>
{
    ["operations"] = ("Operations", "⚙️", FactoryAgentProfiles.Operations.Description),
    ["maintenance"] = ("Maintenance", "🔧", FactoryAgentProfiles.Maintenance.Description),
    ["quality"] = ("Quality", "🔬", FactoryAgentProfiles.Quality.Description),
    ["plant-manager"] = ("Plant Manager", "🏢", FactoryAgentProfiles.PlantManager.Description),
    ["continuous-improvement"] = ("Continuous Improvement", "🔁", FactoryAgentProfiles.ContinuousImprovement.Description),
};

app.MapGet("/api/health", () => Results.Ok(new
{
    runtime = config.Runtime.ToString(),
    modelDeploymentName = config.Runtime == AgentRuntime.Local
        ? config.LocalModelDeploymentName
        : config.ModelDeploymentName,
}));

app.MapGet("/api/agents", () =>
{
    var list = agents.Select(kv => new
    {
        id = kv.Key,
        name = kv.Value.Name,
        displayName = agentDisplay[kv.Key].DisplayName,
        icon = agentDisplay[kv.Key].Icon,
        description = agentDisplay[kv.Key].Description,
        isLocal = kv.Value.IsLocal,
    });
    return Results.Ok(list);
});

app.MapPost("/api/agents/{agentId}/chat", async (string agentId, ChatRequest request, CancellationToken ct) =>
{
    if (!agents.TryGetValue(agentId, out var agent))
    {
        return Results.NotFound(new { error = $"Unknown agent '{agentId}'." });
    }

    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new { error = "message must not be empty." });
    }

    try
    {
        await agent.RegisterAsync(ct);
        var response = await agent.RunAsync(request.Message, ct);
        return Results.Ok(new ChatResponse(response));
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500, title: "Agent invocation failed");
    }
});

app.Run();

internal sealed record ChatRequest(string Message);
internal sealed record ChatResponse(string Response);
