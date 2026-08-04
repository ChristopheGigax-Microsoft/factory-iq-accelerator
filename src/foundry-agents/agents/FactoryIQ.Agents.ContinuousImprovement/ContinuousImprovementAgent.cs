using Azure.AI.Projects;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.ContinuousImprovement;

public sealed class ContinuousImprovementAgent(
    AIProjectClient projectClient,
    AgentRunner agentRunner,
    FoundryConfig config,
    ILogger<ContinuousImprovementAgent> logger)
    : FoundryAgentBase(projectClient, agentRunner, config, logger)
{
    public override string Name => FactoryAgentProfiles.ContinuousImprovement.Name;

    protected override string Description =>
        FactoryAgentProfiles.ContinuousImprovement.Description;

    protected override string Instructions =>
        FactoryAgentProfiles.ContinuousImprovement.CloudInstructions;
}
