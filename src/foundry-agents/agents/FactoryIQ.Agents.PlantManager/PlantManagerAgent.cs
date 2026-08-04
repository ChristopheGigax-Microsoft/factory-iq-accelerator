using Azure.AI.Projects;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.PlantManager;

public sealed class PlantManagerAgent(
    AIProjectClient projectClient,
    AgentRunner agentRunner,
    FoundryConfig config,
    ILogger<PlantManagerAgent> logger)
    : FoundryAgentBase(projectClient, agentRunner, config, logger)
{
    public override string Name => FactoryAgentProfiles.PlantManager.Name;

    protected override string Description =>
        FactoryAgentProfiles.PlantManager.Description;

    protected override bool UsesWorkIqTool => true;

    protected override string Instructions =>
        FactoryAgentProfiles.PlantManager.CloudInstructions;
}
