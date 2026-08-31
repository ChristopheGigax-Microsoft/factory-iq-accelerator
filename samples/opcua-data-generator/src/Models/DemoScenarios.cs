namespace OpcUaDataGenerator.Models;

public enum DemoScenario
{
    /// <summary>Healthy production-line baseline (~95% OEE). Default.</summary>
    Normal,

    /// <summary>
    /// Gradual spindle temperature drift on Tour CNC #1.
    /// +0.5% per tick (capped at +30%). Triggers Maintenance agent investigation.
    /// </summary>
    TemperatureDrift,

    /// <summary>
    /// Quality excursion on PROD-ENGINE-7B batches: 10–25% scrap rate.
    /// Triggers Quality agent root-cause analysis.
    /// </summary>
    QualityExcursion,

    /// <summary>
    /// Unplanned fault on CNC Lathe #2. Forced after 5 ticks.
    /// Triggers Operations agent deviation detection and Plant Manager escalation.
    /// </summary>
    MachineFault,

    /// <summary>
    /// Shift change: all WorkUnits transition Idle → Active over 2 ticks.
    /// Demonstrates the state-change event stream.
    /// </summary>
    ShiftChange
}

public static class ScenarioMetadata
{
    public static string Description(DemoScenario scenario) => scenario switch
    {
        DemoScenario.Normal           => "Normal operation — healthy production-line baseline (~95% OEE)",
        DemoScenario.TemperatureDrift => "Spindle temperature drift on Tour CNC #1 — Maintenance agent detects deviation",
        DemoScenario.QualityExcursion => "Quality excursion on Motor Assembly 7B batches (>10% scrap) — Quality agent investigates",
        DemoScenario.MachineFault     => "Unplanned fault on CNC Lathe #2 — Operations + Plant Manager escalation",
        DemoScenario.ShiftChange      => "Shift change — all equipment transitions Idle → Active",
        _                             => "Unknown scenario"
    };

    /// <summary>Primary WorkUnit affected by the scenario (empty for global scenarios).</summary>
    public static string TargetWorkUnitId(DemoScenario scenario) => scenario switch
    {
        DemoScenario.TemperatureDrift => "wu-lyon-prod-tour1",
        DemoScenario.QualityExcursion => "wu-lyon-qual-cmm1",
        DemoScenario.MachineFault     => "wu-lyon-prod-tour2",
        _                             => string.Empty
    };
}
