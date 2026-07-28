using Isa95DataGenerator.Models;
using Microsoft.Extensions.Logging;

namespace Isa95DataGenerator.Services;

public interface IMachineStateGenerator
{
    IReadOnlyList<TelemetryMessage> GenerateStateChanges();
}

/// <summary>
/// Manages an ISA-95 equipment state machine per WorkUnit and emits EquipmentActual events
/// whenever a transition occurs. State is maintained as a singleton across ticks.
/// </summary>
public class MachineStateGenerator : IMachineStateGenerator
{
    private enum EquipmentState { Active, Idle, Held, Fault, Setup }

    private sealed class WorkUnitState
    {
        public EquipmentState Current = EquipmentState.Active;
        public int TicksInState;
    }

    private readonly IScenarioController _scenario;
    private readonly ILogger<MachineStateGenerator> _logger;
    private readonly Random _rng = new();
    private readonly Dictionary<string, WorkUnitState> _states = [];
    private readonly object _lock = new();
    private int _opSeq;

    public MachineStateGenerator(IScenarioController scenario, ILogger<MachineStateGenerator> logger)
    {
        _scenario = scenario;
        _logger = logger;

        // Pre-initialise all WorkUnits as Active
        foreach (var wu in AllWorkUnits())
            _states[wu.WorkUnitId] = new WorkUnitState();
    }

    public IReadOnlyList<TelemetryMessage> GenerateStateChanges()
    {
        var changes = new List<TelemetryMessage>();
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
        string reason, List<TelemetryMessage> changes, DateTime now)
    {
        _logger.LogInformation("State: {wu} {from} → {to} ({reason})",
            workUnitId, state.Current, next, reason);

        state.Current = next;
        state.TicksInState = 0;

        var label = next switch
        {
            EquipmentState.Active => "Active",
            EquipmentState.Idle   => "Idle",
            EquipmentState.Held   => "Held",
            EquipmentState.Fault  => "Fault",
            EquipmentState.Setup  => "Setup",
            _                     => "Unknown"
        };

        changes.Add(new TelemetryMessage
        {
            Timestamp  = now,
            WorkUnitId = workUnitId,
            Signal     = "State",
            Value      = (double)next,
            Payload    = new EquipmentStatePayload(
                label, reason, $"op-{Interlocked.Increment(ref _opSeq):D4}")
        });
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
