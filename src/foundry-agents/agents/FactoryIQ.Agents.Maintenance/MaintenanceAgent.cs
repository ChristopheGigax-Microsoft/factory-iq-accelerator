using Azure.AI.Projects;
using FactoryIQ.Agents.Shared.Agents;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Maintenance;

public sealed class MaintenanceAgent(
    AIProjectClient projectClient,
    AgentRunner agentRunner,
    FoundryConfig config,
    ILogger<MaintenanceAgent> logger)
    : FoundryAgentBase(projectClient, agentRunner, config, logger)
{
    public override string Name => FactoryAgentProfiles.Maintenance.Name;

    protected override string Description =>
        FactoryAgentProfiles.Maintenance.Description;

    protected override bool UsesWorkIqTool => true;

    protected override string Instructions =>
        FactoryAgentProfiles.Maintenance.CloudInstructions;
}
