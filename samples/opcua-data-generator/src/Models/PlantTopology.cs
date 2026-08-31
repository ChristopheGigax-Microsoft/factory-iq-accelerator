namespace OpcUaDataGenerator.Models;

/// <summary>
/// Continuous signal emitted by a WorkUnit, with realistic operating bounds.
/// </summary>
public sealed record SignalDefinition(
    string Signal,
    string Unit,
    double NominalMin,
    double NominalMax
);

/// <summary>ISA-95 WorkUnit (Level 3b) — individual piece of equipment.</summary>
public sealed record WorkUnit(
    string WorkUnitId,
    string Name,
    string MachineType,
    string WorkCenterId,
    IReadOnlyList<SignalDefinition> Signals
);

/// <summary>ISA-95 WorkCenter (Level 3a) — a group of WorkUnits performing a defined process.</summary>
public sealed record WorkCenter(
    string WorkCenterId,
    string Name,
    string AreaId,
    IReadOnlyList<WorkUnit> WorkUnits,
    IReadOnlyList<ProductDefinition> Products
);

/// <summary>ISA-95 Area (Level 2 segment within a Site).</summary>
public sealed record Area(
    string AreaId,
    string Name,
    string SiteId,
    IReadOnlyList<WorkCenter> WorkCenters
);

/// <summary>ISA-95 Site (Level 2 — a manufacturing facility).</summary>
public sealed record Site(
    string SiteId,
    string Name,
    string Location,
    IReadOnlyList<Area> Areas
);

/// <summary>ISA-95 Enterprise (Level 4) — root of the hierarchy.</summary>
public sealed record Enterprise(
    string EnterpriseId,
    string Name,
    IReadOnlyList<Site> Sites
);

/// <summary>Product manufactured at a WorkCenter, with a standard cycle time for work order simulation.</summary>
public sealed record ProductDefinition(
    string ProductId,
    string Name,
    string MaterialId,    // raw material identifier
    string UnitOfMeasure,
    double StandardCycleTimeSec
);

// ── Demo production line: Lyon Motor Line 1 ───────────────────────────────────

/// <summary>
/// Singleton ISA-95 topology used by the OPC UA generator.
/// Represents one production line controller, not a whole plant-wide OPC UA namespace.
/// Kept as a self-contained copy — intentionally not shared with the ISA-95/IoT Hub generator.
/// </summary>
public static class DemoProductionLine
{
    public static Enterprise Instance { get; } = Build();

    static Enterprise Build() => new(
        "ent-fiq-demo",
        "Factory IQ Demo",
        [BuildLineController()]
    );

    static Site BuildLineController() => new(
        "site-lyon-edge",
        "Lyon Edge Gateway",
        "Lyon, France",
        [BuildMotorLineArea()]
    );

    // ── One OPC UA server scoped to one production line ───────────────────────

    static Area BuildMotorLineArea() => new(
        "area-lyon-motor-line", "Lyon Motor Line 1", "site-lyon-edge",
        [
            new WorkCenter("line-lyon-motor-01", "Lyon Motor Line 1 Controller", "area-lyon-motor-line",
            [
                new WorkUnit("wu-lyon-prod-tour1",  "CNC Lathe #1",        "CNC",       "line-lyon-motor-01", CncSignals()),
                new WorkUnit("wu-lyon-prod-tour2",  "CNC Lathe #2",        "CNC",       "line-lyon-motor-01", CncSignals()),
                new WorkUnit("wu-lyon-prod-rect1",  "Crankshaft Grinder",  "Grinder",   "line-lyon-motor-01", GrinderSignals()),
                new WorkUnit("wu-lyon-qual-cmm1",   "Inline CMM Station",  "CMM",       "line-lyon-motor-01", CmmSignals()),
                new WorkUnit("wu-lyon-qual-bench1", "End-of-Line Test Rig","TestBench", "line-lyon-motor-01", TestBenchSignals())
            ],
            [
                new ProductDefinition("PROD-ENGINE-7B", "Motor Assembly 7B", "MAT-STEEL-42CrMo4", "EA", 480),
                new ProductDefinition("PROD-ENGINE-5A", "Motor Assembly 5A", "MAT-STEEL-42CrMo4", "EA", 360)
            ])
        ]
    );

    // ── Signal definitions per machine type ──────────────────────────────────

    static IReadOnlyList<SignalDefinition> CncSignals() =>
    [
        new("Spindle.Speed",       "rpm",   800,  2800),
        new("Temperature.Spindle", "°C",    20,   75),
        new("Vibration.Velocity",  "mm/s",  0.5,  4.0),
        new("CuttingForce",        "N",     100,  900),
        new("FeedRate",            "mm/min",50,   600),
        new("Coolant.FlowRate",    "L/min", 8,    20)
    ];

    static IReadOnlyList<SignalDefinition> GrinderSignals() =>
    [
        new("Spindle.Speed",       "rpm",   1000, 2800),
        new("Temperature.Spindle", "°C",    20,   70),
        new("Vibration.Velocity",  "mm/s",  0.5,  3.5),
        new("NormalForce",         "N",     20,   180),
        new("WheelWear",           "µm",    0,    50)
    ];

    static IReadOnlyList<SignalDefinition> CmmSignals() =>
    [
        new("ProbeForce",          "mN",    50,   180),
        new("ScanVelocity",        "mm/s",  5,    45),
        new("Vibration.Velocity",  "mm/s",  0.1,  0.8)
    ];

    static IReadOnlyList<SignalDefinition> TestBenchSignals() =>
    [
        new("Engine.Speed",        "rpm",   500,  3500),
        new("Torque.Output",       "Nm",    50,   400),
        new("Temperature.Oil",     "°C",    40,   90),
        new("Vibration.Velocity",  "mm/s",  0.5,  5.0),
        new("Noise.Level",         "dB(A)", 65,   85)
    ];
}
