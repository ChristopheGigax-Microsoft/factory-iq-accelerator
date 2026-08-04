namespace FactoryIQ.Agents.Shared.Local.Tools.Contracts;

public sealed record EquipmentStatus(string EquipmentId, string Status, DateTimeOffset ObservedAt);

public sealed record MachineAlarm(
    string EquipmentId,
    string AlarmCode,
    string Description,
    string Severity,
    DateTimeOffset OccurredAt);

public sealed record TelemetryPoint(
    string EquipmentId,
    string Metric,
    double Value,
    string Unit,
    DateTimeOffset Timestamp);

public sealed record PerformanceSummary(
    string ScopeId,
    double Oee,
    double Availability,
    double Performance,
    double Quality,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd);

public sealed record LocalDocument(string Id, string Name, string Content);

public interface IEquipmentOperations
{
    Task<EquipmentStatus?> GetEquipmentStatusAsync(string equipmentId, CancellationToken ct = default);
}

public interface IAlarmOperations
{
    Task<IReadOnlyList<MachineAlarm>> GetActiveAlarmsAsync(
        string? equipmentId = null,
        CancellationToken ct = default);
}

public interface ITelemetryOperations
{
    Task<IReadOnlyList<TelemetryPoint>> GetTelemetryAsync(
        string equipmentId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);
}

public interface IPerformanceOperations
{
    Task<PerformanceSummary?> GetPerformanceAsync(
        string scopeId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);
}

public interface ILocalFileOperations
{
    Task<IReadOnlyList<LocalDocument>> SearchDocumentsAsync(
        string query,
        CancellationToken ct = default);
}
