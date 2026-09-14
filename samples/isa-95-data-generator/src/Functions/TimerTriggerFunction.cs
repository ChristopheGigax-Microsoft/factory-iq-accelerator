using Isa95DataGenerator.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Isa95DataGenerator.Functions;

public class TimerTriggerFunction
{
    private readonly ILogger<TimerTriggerFunction> _logger;
    private readonly IIoTHubService _iotHub;
    private readonly ITelemetryGenerator _telemetry;
    private readonly IMachineStateGenerator _machineState;
    private readonly IWorkOrderOrchestrator _workOrders;

    public TimerTriggerFunction(
        ILogger<TimerTriggerFunction> logger,
        IIoTHubService iotHub,
        ITelemetryGenerator telemetry,
        IMachineStateGenerator machineState,
        IWorkOrderOrchestrator workOrders)
    {
        _logger = logger;
        _iotHub = iotHub;
        _telemetry = telemetry;
        _machineState = machineState;
        _workOrders = workOrders;
    }

    /// <summary>
    /// Fast tick every 10 seconds.
    /// Generates Equipment Telemetry signals and Equipment Actual state events.
    /// Sends source events through the RawTelemetry Bronze envelope.
    /// </summary>
    [Function(nameof(TelemetryTick))]
    public async Task TelemetryTick([TimerTrigger("*/10 * * * * *")] TimerInfo timer)
    {
        var signals      = _telemetry.GenerateSignals();
        var stateChanges = _machineState.GenerateStateChanges();
        var all          = signals.Concat(stateChanges).ToList();

        foreach (var msg in all)
            await _iotHub.SendMessageAsync(msg);

        _logger.LogInformation(
            "TelemetryTick: {signals} signals + {states} state events → {total} messages sent",
            signals.Count, stateChanges.Count, all.Count);
    }

    /// <summary>
    /// Slow tick every 60 seconds.
    /// Creates/closes work orders and emits material actuals + quality test results.
    /// Sends work, material, and quality source events through RawTelemetry.
    /// </summary>
    [Function(nameof(WorkOrderTick))]
    public async Task WorkOrderTick([TimerTrigger("0 */1 * * * *")] TimerInfo timer)
    {
        var messages = _workOrders.ProcessTick();

        foreach (var msg in messages)
            await _iotHub.SendMessageAsync(msg);

        if (messages.Count > 0)
            _logger.LogInformation(
                "WorkOrderTick: {count} messages (WR/WRS/MaterialActual/QualityTest)", messages.Count);
    }
}
