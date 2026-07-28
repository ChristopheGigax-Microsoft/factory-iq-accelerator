namespace Isa95DataGenerator.Models;

/// <summary>
/// Single wire envelope sent to IoT Hub and routed through Fabric Eventstream.
/// Maps directly to TelemetryLanding Bronze table columns.
/// KQL update policies dispatch to Silver tables based on Payload field presence:
///   - Signal + Value only          → EquipmentTelemetry (always)
///   - Payload.State present        → EquipmentActual
///   - Payload.RequestId present    → WorkRequest
///   - Payload.ResponseId present   → WorkResponse
///   - Payload.LotId + Direction    → MaterialActual
///   - Payload.TestId present       → QualityTestResult
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

/// <summary>Feeds EquipmentActual when Payload.State is non-empty.</summary>
public sealed record EquipmentStatePayload(
    string State,        // Active | Idle | Held | Fault | Setup
    string StateReason,  // ProductionOrder | PlannedMaintenance | UnplannedFault | ChangeOver | Breakdown
    string OperatorId
);

// ── ISA-95 Part 4 – Work Request ─────────────────────────────────────────────

/// <summary>Feeds WorkRequest when Payload.RequestId is non-empty.</summary>
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

/// <summary>Feeds WorkResponse when Payload.ResponseId is non-empty.</summary>
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

/// <summary>Feeds MaterialActual when Payload.LotId and Payload.Direction are non-empty.</summary>
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

/// <summary>Feeds QualityTestResult when Payload.TestId is non-empty.</summary>
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
