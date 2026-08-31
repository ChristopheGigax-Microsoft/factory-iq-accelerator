using OpcUaDataGenerator.Models;
using Microsoft.Extensions.Logging;

namespace OpcUaDataGenerator.Services;

/// <summary>A WorkUnit state/alarm transition to apply to OPC UA nodes.</summary>
public sealed record StateChange(string WorkUnitId, EquipmentState State, string Reason, DateTime Timestamp);

public interface IMachineStateGenerator
{
    IReadOnlyList<StateChange> GenerateStateChanges();

    /// <summary>Active alarms across all WorkUnits, refreshed after each call to <see cref="GenerateStateChanges"/>.</summary>
    IReadOnlyList<MachineAlarmState> ActiveAlarms { get; }
}

/// <summary>
/// Manages an ISA-95 equipment state machine per WorkUnit and emits state transitions
/// (written to OPC UA nodes by the server host). State is maintained as a singleton across ticks.
/// Fault transitions also raise/clear a corresponding OPC UA alarm/condition.
/// </summary>
public class MachineStateGenerator : IMachineStateGenerator
{
    private sealed class WorkUnitState
    {
        public EquipmentState Current = EquipmentState.Active;
        public int TicksInState;
    }

    private readonly IScenarioController _scenario;
    private readonly ILogger<MachineStateGenerator> _logger;
    private readonly Random _rng = new();
    private readonly Dictionary<string, WorkUnitState> _states = [];
    private readonly Dictionary<string, MachineAlarmState> _alarms = [];
    private readonly object _lock = new();
    private int _alarmSeq;

    public MachineStateGenerator(IScenarioController scenario, ILogger<MachineStateGenerator> logger)
    {
        _scenario = scenario;
        _logger = logger;

        // Pre-initialise all WorkUnits as Active
        foreach (var wu in AllWorkUnits())
            _states[wu.WorkUnitId] = new WorkUnitState();
    }

    public IReadOnlyList<MachineAlarmState> ActiveAlarms
    {
        get { lock (_lock) return _alarms.Values.Where(a => a.Active).ToList(); }
    }

    public IReadOnlyList<StateChange> GenerateStateChanges()
    {
        var changes = new List<StateChange>();
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            foreach (var wu in AllWorkUnits())
            {
                if (!_states.TryGetValue(wu.WorkUnitId, out var state))
                {
                    state = new WorkUnitState();
                    _states[wu.WorkUnitId] = state;
                }

                state.TicksInState++;

                // MachineFault scenario: force a fault on the target WorkUnit
                if (_scenario.ShouldForceFault(wu.WorkUnitId) &&
                    state.Current == EquipmentState.Active &&
                    state.TicksInState > 5)
                {
                    Transition(wu.WorkUnitId, state, EquipmentState.Fault, "Breakdown", changes, now);
                    continue;
                }

                // ShiftChange scenario: push all machines to Active
                if (_scenario.Current == DemoScenario.ShiftChange &&
                    state.Current == EquipmentState.Idle &&
                    state.TicksInState >= 2)
                {
                    Transition(wu.WorkUnitId, state, EquipmentState.Active, "ShiftStart", changes, now);
                    continue;
                }

                var next = RollTransition(state);
                if (next.HasValue)
                    Transition(wu.WorkUnitId, state, next.Value.next, next.Value.reason, changes, now);
            }
        }

        return changes;
    }

    // ── Transition probabilities per state ───────────────────────────────────

    (EquipmentState next, string reason)? RollTransition(WorkUnitState s)
    {
        var roll = _rng.NextDouble();
        return s.Current switch
        {
            EquipmentState.Active => roll switch
            {
                < 0.010 => (EquipmentState.Fault, "UnplannedFault"),
                < 0.025 => (EquipmentState.Held,  "OperatorHold"),
                < 0.045 => (EquipmentState.Idle,  "ProductionComplete"),
                _       => null
            },
            EquipmentState.Idle when s.TicksInState >= 2 => roll switch
            {
                < 0.70  => (EquipmentState.Active, "ProductionOrder"),
                < 0.75  => (EquipmentState.Setup,  "ChangeOver"),
                _       => null
            },
            EquipmentState.Fault when s.TicksInState >= 3 => roll switch
            {
                < 0.15  => (EquipmentState.Idle, "FaultCleared"),
                < 0.25  => (EquipmentState.Idle, "MaintenanceComplete"),
                _       => null
            },
            EquipmentState.Held when s.TicksInState >= 2 => roll switch
            {
                < 0.50  => (EquipmentState.Active, "HoldReleased"),
                _       => null
            },
            EquipmentState.Setup when s.TicksInState >= 4 => roll switch
            {
                < 0.30  => (EquipmentState.Active, "SetupComplete"),
                _       => null
            },
            _ => null
        };
    }

    void Transition(string workUnitId, WorkUnitState state, EquipmentState next,
        string reason, List<StateChange> changes, DateTime now)
    {
        _logger.LogInformation("State: {wu} {from} → {to} ({reason})",
            workUnitId, state.Current, next, reason);

        var previous = state.Current;
        state.Current = next;
        state.TicksInState = 0;

        changes.Add(new StateChange(workUnitId, next, reason, now));

        // Raise an alarm/condition when entering Fault; clear it when leaving Fault.
        if (next == EquipmentState.Fault)
        {
            _alarms[workUnitId] = new MachineAlarmState(
                workUnitId,
                AlarmCode: $"ALM-{Interlocked.Increment(ref _alarmSeq):D4}",
                Description: $"Equipment fault: {reason}",
                Severity: "High",
                OccurredAt: now,
                Active: true);
        }
        else if (previous == EquipmentState.Fault && _alarms.TryGetValue(workUnitId, out var existing))
        {
            _alarms[workUnitId] = existing with { Active = false };
        }
    }

    static IEnumerable<WorkUnit> AllWorkUnits()
    {
        foreach (var site in DemoPlant.Instance.Sites)
        foreach (var area in site.Areas)
        foreach (var wc in area.WorkCenters)
        foreach (var wu in wc.WorkUnits)
            yield return wu;
    }
}
