using Isa95DataGenerator.Models;
using Microsoft.Extensions.Logging;

namespace Isa95DataGenerator.Services;

public interface ITelemetryGenerator
{
    IReadOnlyList<TelemetryMessage> GenerateSignals();
}

/// <summary>
/// Generates continuous ISA-95 Equipment Telemetry for all WorkUnits.
/// Maintains anomaly state (spike / sustained-high / near-zero) per signal across ticks.
/// </summary>
public class TelemetryGenerator : ITelemetryGenerator
{
    private readonly IScenarioController _scenario;
    private readonly ILogger<TelemetryGenerator> _logger;
    private readonly Random _rng = new();
    private readonly Dictionary<string, AnomalyState> _anomalies = [];
    private readonly object _lock = new();
    private int _tickCount;

    public TelemetryGenerator(IScenarioController scenario, ILogger<TelemetryGenerator> logger)
    {
        _scenario = scenario;
        _logger = logger;
    }

    public IReadOnlyList<TelemetryMessage> GenerateSignals()
    {
        int tick;
        lock (_lock) tick = ++_tickCount;

        var messages = new List<TelemetryMessage>();
        var now = DateTime.UtcNow;

        foreach (var site in DemoPlant.Instance.Sites)
        foreach (var area in site.Areas)
        foreach (var wc in area.WorkCenters)
        foreach (var wu in wc.WorkUnits)
        foreach (var signal in wu.Signals)
        {
            var value = ComputeValue(wu.WorkUnitId, signal, tick);
            messages.Add(new TelemetryMessage
            {
                Timestamp = now,
                WorkUnitId = wu.WorkUnitId,
                Signal = signal.Signal,
                Value = Math.Round(value, 4),
                Payload = null
            });
        }

        _logger.LogDebug("TelemetryGenerator: {count} signals (tick {tick})", messages.Count, tick);
        return messages;
    }

    double ComputeValue(string workUnitId, SignalDefinition signal, int tick)
    {
        var range = signal.NominalMax - signal.NominalMin;
        var nominal = signal.NominalMin + _rng.NextDouble() * range;

        // Apply scenario-driven temperature drift on the target WorkUnit
        if (signal.Signal is "Temperature.Spindle" or "Temperature.Oil")
            nominal *= _scenario.GetTemperatureDriftMultiplier(workUnitId, tick);

        var key = $"{workUnitId}:{signal.Signal}";
        AnomalyState anomaly;
        lock (_lock)
        {
            if (!_anomalies.TryGetValue(key, out anomaly!))
            {
                anomaly = new AnomalyState();
                _anomalies[key] = anomaly;
            }
        }

        return anomaly.Apply(nominal, _rng);
    }
}

internal sealed class AnomalyState
{
    enum Kind { Normal, SustainedHigh, SustainedLow }

    Kind _current = Kind.Normal;
    int _remaining;

    public double Apply(double nominal, Random rng)
    {
        // Currently in an anomaly state — count down remaining ticks
        if (_current != Kind.Normal)
        {
            _remaining--;
            if (_remaining <= 0) _current = Kind.Normal;

            return _current == Kind.SustainedHigh
                ? nominal * (2.5 + rng.NextDouble())          // ×2.5–3.5
                : nominal * (0.03 + rng.NextDouble() * 0.07); // ×0.03–0.10
        }

        // Decide which anomaly (if any) to enter this tick
        var roll = rng.NextDouble();

        if (roll < 0.020) // 2.0% — instantaneous spike
            return nominal * (3.5 + rng.NextDouble() * 1.5);

        if (roll < 0.035) // 1.5% — sustained high (5–15 ticks)
        {
            _current = Kind.SustainedHigh;
            _remaining = rng.Next(5, 15);
            return nominal * (2.5 + rng.NextDouble());
        }

        if (roll < 0.040) // 0.5% — near-zero (5–15 ticks)
        {
            _current = Kind.SustainedLow;
            _remaining = rng.Next(5, 15);
            return nominal * (0.03 + rng.NextDouble() * 0.07);
        }

        return nominal;
    }
}
