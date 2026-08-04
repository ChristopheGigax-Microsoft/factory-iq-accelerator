using Azure.AI.Projects;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Quality;

public sealed class QualityAgent(
    AIProjectClient projectClient,
    AgentRunner agentRunner,
    FoundryConfig config,
    ILogger<QualityAgent> logger)
    : FoundryAgentBase(projectClient, agentRunner, config, logger)
{
    public override string Name => FactoryAgentProfiles.Quality.Name;

    protected override string Description =>
        FactoryAgentProfiles.Quality.Description;

    protected override string Instructions =>
        FactoryAgentProfiles.Quality.CloudInstructions;
}
