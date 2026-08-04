using Azure.AI.Projects;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Operations;

public sealed class OperationsAgent(
    AIProjectClient projectClient,
    AgentRunner agentRunner,
    FoundryConfig config,
    ILogger<OperationsAgent> logger)
    : FoundryAgentBase(projectClient, agentRunner, config, logger)
{
    public override string Name => FactoryAgentProfiles.Operations.Name;

    protected override string Description =>
        FactoryAgentProfiles.Operations.Description;

    protected override string Instructions =>
        FactoryAgentProfiles.Operations.CloudInstructions;
}
