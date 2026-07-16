namespace FactoryIQ.Agents.Shared.Models;

/// <summary>
/// Configuration loaded from environment variables for Azure AI Foundry.
/// </summary>
public sealed record FoundryConfig
{
    public required string ProjectEndpoint { get; init; }
    public required string ModelDeploymentName { get; init; }
    public required string SearchEndpoint { get; init; }
    public required string KnowledgeBaseName { get; init; }
    public required string KnowledgeBaseProjectConnectionName { get; init; }
    public required string FabricDataAgentProjectConnectionName { get; init; }
    public bool DeletePersistentAgentOnExit { get; init; }
}

/// <summary>
/// Alert raised when a deviation or anomaly is detected.
/// </summary>
public sealed record PlantAlert
{
    public required string MachineId { get; init; }
    public required string AlertType { get; init; }
    public required string Severity { get; init; }
    public required string Description { get; init; }
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object> Metadata { get; init; } = [];
}

/// <summary>
/// KPI summary for a plant or work center.
/// </summary>
public sealed record KpiSummary
{
    public required string EntityId { get; init; }
    public required string EntityName { get; init; }
    public double Oee { get; init; }
    public double Availability { get; init; }
    public double Performance { get; init; }
    public double Quality { get; init; }
    public DateTimeOffset PeriodStart { get; init; }
    public DateTimeOffset PeriodEnd { get; init; }
}

/// <summary>
/// Work order for maintenance tasks.
/// </summary>
public sealed record WorkOrder
{
    public string? Id { get; set; }
    public string? WorkOrderNumber { get; set; }
    public required string MachineId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; set; }
    public string Type { get; set; } = "corrective";
    public string Priority { get; set; } = "medium";
    public string Status { get; set; } = "new";
    public string? AssignedTo { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Quality defect record.
/// </summary>
public sealed record QualityDefect
{
    public required string DefectId { get; init; }
    public required string MachineId { get; init; }
    public required string DefectType { get; init; }
    public required string BatchId { get; init; }
    public string? RootCause { get; set; }
    public string Severity { get; set; } = "medium";
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Improvement opportunity identified by CI agent.
/// </summary>
public sealed record ImprovementOpportunity
{
    public required string Title { get; init; }
    public required string Category { get; init; }
    public required string Description { get; init; }
    public double EstimatedImpactPercent { get; init; }
    public string Priority { get; set; } = "medium";
    public List<string> AffectedAssets { get; init; } = [];
}
