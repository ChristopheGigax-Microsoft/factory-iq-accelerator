using OpcUaDataGenerator.Models;

namespace OpcUaDataGenerator.Services;

public interface IScenarioController
{
    DemoScenario Current { get; }

    /// <summary>Temperature drift multiplier for the target WorkUnit (1.0 = no drift).</summary>
    double GetTemperatureDriftMultiplier(string workUnitId, int tick);

    /// <summary>True when the quality excursion scenario should force high scrap for this product.</summary>
    bool ShouldForceScrap(string productId);

    /// <summary>True when the machine fault scenario should force a fault on this WorkUnit.</summary>
    bool ShouldForceFault(string workUnitId);
}

public class ScenarioController : IScenarioController
{
    public DemoScenario Current { get; }

    public ScenarioController()
    {
        var raw = Environment.GetEnvironmentVariable("DEMO_SCENARIO") ?? "Normal";
        Current = Enum.TryParse<DemoScenario>(raw, ignoreCase: true, out var parsed)
            ? parsed
            : DemoScenario.Normal;
    }

    public double GetTemperatureDriftMultiplier(string workUnitId, int tick)
    {
        if (Current != DemoScenario.TemperatureDrift) return 1.0;
        if (workUnitId != ScenarioMetadata.TargetWorkUnitId(DemoScenario.TemperatureDrift)) return 1.0;
        // +0.5% per fast tick, capped at +30%
        return 1.0 + Math.Min(tick * 0.005, 0.30);
    }

    public bool ShouldForceScrap(string productId) =>
        Current == DemoScenario.QualityExcursion && productId == "PROD-ENGINE-7B";

    public bool ShouldForceFault(string workUnitId) =>
        Current == DemoScenario.MachineFault &&
        workUnitId == ScenarioMetadata.TargetWorkUnitId(DemoScenario.MachineFault);
}
