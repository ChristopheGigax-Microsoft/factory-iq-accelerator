using FactoryIQ.Agents.Shared.Local.Tools.Contracts;

namespace FactoryIQ.Agents.Shared.Local.Tools.Mqtt;

public sealed class MqttMachineDataTool :
    IEquipmentOperations,
    IAlarmOperations,
    ITelemetryOperations,
    IPerformanceOperations
{
    public Task<EquipmentStatus?> GetEquipmentStatusAsync(
        string equipmentId,
        CancellationToken ct = default) =>
        throw new LocalToolNotImplementedException(nameof(MqttMachineDataTool));

    public Task<IReadOnlyList<MachineAlarm>> GetActiveAlarmsAsync(
        string? equipmentId = null,
        CancellationToken ct = default) =>
        throw new LocalToolNotImplementedException(nameof(MqttMachineDataTool));

    public Task<IReadOnlyList<TelemetryPoint>> GetTelemetryAsync(
        string equipmentId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default) =>
        throw new LocalToolNotImplementedException(nameof(MqttMachineDataTool));

    public Task<PerformanceSummary?> GetPerformanceAsync(
        string scopeId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default) =>
        throw new LocalToolNotImplementedException(nameof(MqttMachineDataTool));
}
