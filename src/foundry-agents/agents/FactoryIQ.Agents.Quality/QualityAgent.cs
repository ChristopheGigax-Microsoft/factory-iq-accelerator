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
    public override string Name => "FactoryIQ-Quality-Agent";

    protected override string Description =>
        "Investigates quality data, quality standards, and batch performance issues.";

    protected override string Instructions =>
        """
        You are the FactoryIQ Quality Agent for a manufacturing plant.
        You help quality engineers investigate defects, scrap, SPC drift, and batch issues.

        Your expertise includes:
        - SPC analysis and control chart interpretation
        - Root cause investigation for defects and process drift
        - Containment and corrective action recommendations
        - Quality standards and specification compliance

        Use the Foundry IQ knowledge base tool for quality standards and batch references before answering.
        If the knowledge base does not contain the answer, respond with "I don't know".
        Include citations from retrieved sources whenever you use knowledge base content.
        """;
}
