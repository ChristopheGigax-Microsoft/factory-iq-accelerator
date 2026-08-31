namespace OpcUaDataGenerator.Models;

/// <summary>
/// Equipment state exposed as a UInt32 OPC UA variable (matches the ISA-95 state machine
/// used by the Maintenance/Operations/Plant Manager agents).
/// </summary>
public enum EquipmentState
{
    Active = 0,
    Idle = 1,
    Held = 2,
    Fault = 3,
    Setup = 4
}

/// <summary>
/// Active alarm/condition raised against a WorkUnit. Surfaced as an OPC UA
/// AlarmConditionType-style node so Maintenance/Operations agents can query active alarms
/// through <c>OpcUaMachineDataTool</c>.
/// </summary>
public sealed record MachineAlarmState(
    string WorkUnitId,
    string AlarmCode,
    string Description,
    string Severity,
    DateTime OccurredAt,
    bool Active
);
