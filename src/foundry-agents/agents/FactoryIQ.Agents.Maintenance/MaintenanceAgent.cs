using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using FactoryIQ.Agents.Shared.Models;
using FactoryIQ.Agents.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FactoryIQ.Agents.Maintenance;

/// <summary>
/// Maintenance Agent: correlates alarms, asset history, work orders, and sensor trends.
/// Uses Fabric Data Agent for sensor KQL data, Foundry IQ (AI Search) for maintenance procedures
/// and runbooks, and Work IQ for work order management.
/// </summary>
public sealed class MaintenanceAgent(
    AIProjectClient projectClient,
    FabricDataAgentService fabricDataAgent,
    KnowledgeSearchService knowledgeSearch,
    FoundryConfig config,
    ILogger<MaintenanceAgent> logger)
{
    private const string AgentName = "MaintenanceAgent";
    private const string Instructions = """
        You are a Maintenance Agent for a manufacturing plant. Your role is to:
        1. Correlate alarms from multiple sources (sensors, PLCs, SCADA)
        2. Analyze asset history and maintenance records
        3. Cross-reference work orders to identify patterns
        4. Monitor sensor trends for predictive maintenance signals

        You have access to:
        - query_sensor_data: Query real-time and historical sensor data from the KQL database
        - search_maintenance_docs: Search maintenance procedures, runbooks, and OEM manuals (Foundry IQ)
        - query_work_orders: Query open and historical work orders (Work IQ)
        - get_asset_history: Retrieve maintenance history for a specific asset

        When correlating alarms:
        - Group related alarms by time window and asset proximity
        - Look for cascade patterns (one failure triggering others)
        - Check maintenance history for recurring issues
        - Reference runbooks for known alarm-to-root-cause mappings

        Output format:
        {
            "machineId": "<id>",
            "correlationWindow": "<timespan>",
            "alarmGroups": [{ "alarms": [...], "likelyCause": "<cause>", "confidence": <0-1> }],
            "maintenanceRecommendation": {
                "urgency": "immediate" | "scheduled" | "monitor",
                "action": "<description>",
                "procedure": "<reference to maintenance doc>",
                "estimatedDowntime": "<duration>"
            },
            "relatedWorkOrders": ["<WO-number>"],
            "summary": "<human-readable summary>"
        }
        """;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Initializing {AgentName}", AgentName);
        var definition = new PromptAgentDefinition(model: config.ModelDeploymentName) { Instructions = Instructions };
        await projectClient.Agents.CreateAgentVersionAsync(AgentName, new AgentVersionCreationOptions(definition), ct);
        logger.LogInformation("✅ {AgentName} initialized", AgentName);
    }

    public async Task<string> CorrelateAlarmsAsync(string machineId, TimeSpan window, CancellationToken ct = default)
    {
        logger.LogInformation("Correlating alarms for {MachineId} over {Window}", machineId, window);

        var sensorData = await fabricDataAgent.QueryAsync(
            $"Get all alarms and sensor anomalies for machine {machineId} in the last {window.TotalHours} hours, including temperature, vibration, pressure readings",
            ct);

        var workOrderHistory = await fabricDataAgent.QueryAsync(
            $"Get work order history for machine {machineId} in the last 90 days",
            ct);

        var procedures = await knowledgeSearch.SearchAsync(
            $"maintenance procedure alarm correlation troubleshooting {machineId}", maxResults: 4, ct: ct);

        var knowledgeContext = string.Join("\n---\n", procedures.Select(p => $"[{p.Title}]: {p.Content}"));

        var prompt = $"""
            Correlate the following alarms and sensor data for machine {machineId}:

            ## Recent Sensor Data & Alarms
            {sensorData}

            ## Work Order History
            {workOrderHistory}

            ## Maintenance Procedures & Runbooks
            {knowledgeContext}

            Analyze patterns, identify root causes, and recommend maintenance actions.
            Correlation window: {window.TotalHours} hours
            """;

        var agent = projectClient.GetAIAgent(name: AgentName, cancellationToken: ct);
        var response = await agent.RunAsync(prompt, thread: null, options: null, cancellationToken: ct);
        return response.Text ?? "No correlation result generated.";
    }
}
