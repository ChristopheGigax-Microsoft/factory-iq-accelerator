namespace Isa95DataGenerator.Models;

/// <summary>
/// Stable Bronze envelope sent to IoT Hub and mapped directly to RawTelemetry.
/// Payload preserves the complete source-system event.
/// </summary>
public sealed record RawTelemetryEnvelope
{
    public required DateTime IngestedAt { get; init; }
    public required DateTime EventTime { get; init; }
    public required string SourceSystem { get; init; }
    public required string SourceSchema { get; init; }
    public required string SourceRecordId { get; init; }
    public required TelemetryMessage Payload { get; init; }
}

/// <summary>
/// Demo source event. The optional routing profile interprets these events;
/// the accelerator's Bronze layer does not impose their semantics.
/// </summary>
public sealed record TelemetryMessage
{
    public required DateTime Timestamp { get; init; }
    public required string WorkUnitId { get; init; }
    public required string Signal { get; init; }
    public required double Value { get; init; }
    public object? Payload { get; init; }
}

// ── ISA-95 Part 4 – Equipment Performance ────────────────────────────────────

/// <summary>Demo equipment-state payload.</summary>
public sealed record EquipmentStatePayload(
    string State,        // Active | Idle | Held | Fault | Setup
    string StateReason,  // ProductionOrder | PlannedMaintenance | UnplannedFault | ChangeOver | Breakdown
    string OperatorId
);

// ── ISA-95 Part 4 – Work Request ─────────────────────────────────────────────

/// <summary>Demo work-request payload.</summary>
public sealed record WorkRequestPayload(
    string RequestId,
    string WorkCenterId,
    string ProductId,
    double QuantityRequested,
    string UnitOfMeasure,
    int Priority,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    string Status,       // Pending | Active | Completed | Cancelled
    DateTime CreatedAt
);

// ── ISA-95 Part 4 – Work Response ────────────────────────────────────────────

/// <summary>Demo work-response payload.</summary>
public sealed record WorkResponsePayload(
    string ResponseId,
    string RequestId,
    string WorkCenterId,
    DateTime ActualStart,
    DateTime? ActualEnd,
    double QuantityProduced,
    double QuantityRejected,
    string Status,       // InProgress | Completed | Partial
    DateTime? CompletedAt
);

// ── ISA-95 Part 4 – Material Actual ──────────────────────────────────────────

/// <summary>Demo material-actual payload.</summary>
public sealed record MaterialActualPayload(
    string LotId,
    string MaterialDefinitionId,
    string WorkCenterId,
    string RequestId,
    string Direction,    // Consumed | Produced
    double Quantity,
    string UnitOfMeasure
);

// ── ISA-95 Part 5 – Quality Test Result ──────────────────────────────────────

/// <summary>Demo quality-test payload.</summary>
public sealed record QualityTestPayload(
    string TestId,
    string WorkUnitId,
    string ResponseId,
    string LotId,
    string TestSpecificationId,
    string Parameter,
    double MeasuredValue,
    double LowerLimit,
    double UpperLimit,
    string UnitOfMeasure,
    string Result,       // Pass | Fail | Warning
    string Severity      // None | Minor | Major | Critical
);
