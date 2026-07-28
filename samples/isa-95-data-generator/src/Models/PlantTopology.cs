namespace Isa95DataGenerator.Models;

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

// ── Demo Plant: Usine Lyon ────────────────────────────────────────────────────

/// <summary>
/// Singleton ISA-95 topology used by all generators.
/// Represents Usine Lyon with three areas: Production Moteurs, Contrôle Qualité, Usinage Vilebrequins.
/// </summary>
public static class DemoPlant
{
    public static Enterprise Instance { get; } = Build();

    static Enterprise Build() => new(
        "ent-fiq-demo",
        "Factory IQ Demo",
        [BuildLyon()]
    );

    static Site BuildLyon() => new(
        "site-lyon",
        "Usine Lyon",
        "Lyon, France",
        [BuildProductionArea(), BuildQualityArea(), BuildCrankshaftArea()]
    );

    // ── Area 1: Production Moteurs ────────────────────────────────────────────

    static Area BuildProductionArea() => new(
        "area-lyon-production", "Production Moteurs", "site-lyon",
        [
            new WorkCenter("wc-lyon-prod-01", "Ligne Production Moteurs", "area-lyon-production",
            [
                new WorkUnit("wu-lyon-prod-tour1", "Tour CNC #1",    "CNC",     "wc-lyon-prod-01", CncSignals()),
                new WorkUnit("wu-lyon-prod-tour2", "Tour CNC #2",    "CNC",     "wc-lyon-prod-01", CncSignals()),
                new WorkUnit("wu-lyon-prod-rect1", "Rectifieuse #1", "Grinder", "wc-lyon-prod-01", GrinderSignals())
            ],
            [
                new ProductDefinition("PROD-CRANK-7B", "Vilebrequin 7B", "MAT-STEEL-42CrMo4", "EA", 480),
                new ProductDefinition("PROD-CRANK-5A", "Vilebrequin 5A", "MAT-STEEL-42CrMo4", "EA", 360)
            ])
        ]
    );

    // ── Area 2: Contrôle Qualité ─────────────────────────────────────────────

    static Area BuildQualityArea() => new(
        "area-lyon-quality", "Contrôle Qualité", "site-lyon",
        [
            new WorkCenter("wc-lyon-qual-01", "Contrôle Qualité", "area-lyon-quality",
            [
                new WorkUnit("wu-lyon-qual-cmm1",   "Machine CMM #1",       "CMM",      "wc-lyon-qual-01", CmmSignals()),
                new WorkUnit("wu-lyon-qual-bench1",  "Banc de Test Moteur #1", "TestBench", "wc-lyon-qual-01", TestBenchSignals())
            ],
            []) // Quality area doesn't manage production orders
        ]
    );

    // ── Area 3: Usinage Vilebrequins ─────────────────────────────────────────

    static Area BuildCrankshaftArea() => new(
        "area-lyon-crankshaft", "Usinage Vilebrequins", "site-lyon",
        [
            new WorkCenter("wc-lyon-crank-01", "Usinage Vilebrequins", "area-lyon-crankshaft",
            [
                new WorkUnit("wu-lyon-crank-centre1", "Centre d'Usinage #1", "CNC", "wc-lyon-crank-01", CncSignals()),
                new WorkUnit("wu-lyon-crank-tour1",   "Tour Vertical #1",   "CNC", "wc-lyon-crank-01", CncSignals())
            ],
            [
                new ProductDefinition("PROD-CRANK-7B",  "Vilebrequin 7B", "MAT-STEEL-42CrMo4", "EA", 600),
                new ProductDefinition("PROD-PISTON-X2", "Piston X2",      "MAT-ALUM-2024",     "EA", 240)
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
