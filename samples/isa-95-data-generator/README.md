# ISA-95 Data Generator

Demo data generator for the **Factory IQ Accelerator**. Simulates a complete ISA-95-compliant manufacturing plant and pumps realistic data through the accelerator's ingestion pipeline to power the Foundry agents demo.

## Architecture

```
Azure Function (timer × 2)
  └─→ Azure IoT Hub
        └─→ Fabric Eventstream (Custom Source: IoT Hub)
              └─→ TelemetryLanding (Bronze KQL table)
                    └─→ Silver tables (KQL update policies)
                          ├─ EquipmentTelemetry
                          ├─ EquipmentActual
                          ├─ WorkRequest
                          ├─ WorkResponse
                          ├─ MaterialActual
                          └─ QualityTestResult
                                └─→ Foundry Agents (Operations, Maintenance, Quality…)
```

## ISA-95 Topology (Usine Lyon)

```
Enterprise: ent-fiq-demo
└── Site: site-lyon (Usine Lyon, Lyon France)
    ├── Area: area-lyon-production (Production Moteurs)
    │   └── WorkCenter: wc-lyon-prod-01
    │       ├── WorkUnit: wu-lyon-prod-tour1  — Tour CNC #1   [CNC]
    │       ├── WorkUnit: wu-lyon-prod-tour2  — Tour CNC #2   [CNC]
    │       └── WorkUnit: wu-lyon-prod-rect1  — Rectifieuse #1 [Grinder]
    ├── Area: area-lyon-quality (Contrôle Qualité)
    │   └── WorkCenter: wc-lyon-qual-01
    │       ├── WorkUnit: wu-lyon-qual-cmm1   — Machine CMM #1     [CMM]
    │       └── WorkUnit: wu-lyon-qual-bench1 — Banc de Test #1    [TestBench]
    └── Area: area-lyon-crankshaft (Usinage Vilebrequins)
        └── WorkCenter: wc-lyon-crank-01
            ├── WorkUnit: wu-lyon-crank-centre1 — Centre d'Usinage #1 [CNC]
            └── WorkUnit: wu-lyon-crank-tour1   — Tour Vertical #1    [CNC]
```

## Message Format

Official reference used for this demo message design (ISA-95/B2MML):  
https://github.com/MESAInternational/B2MML-BatchML

Concrete schema files used for the mapping below:

| Demo concept | B2MML schema |
|---|---|
| Equipment state / equipment context | https://github.com/MESAInternational/B2MML-BatchML/blob/master/Schema/B2MML-Equipment.xsd |
| Work request / schedule | https://github.com/MESAInternational/B2MML-BatchML/blob/master/Schema/B2MML-OperationsSchedule.xsd |
| Work response / execution feedback | https://github.com/MESAInternational/B2MML-BatchML/blob/master/Schema/B2MML-OperationsPerformance.xsd |
| Material actual / consumption-production | https://github.com/MESAInternational/B2MML-BatchML/blob/master/Schema/B2MML-Material.xsd |
| Quality test / quality event | https://github.com/MESAInternational/B2MML-BatchML/blob/master/Schema/B2MML-OperationsTest.xsd |

All messages use a single `TelemetryLanding` envelope. The KQL update policies dispatch to Silver tables based on which `Payload` fields are present:

| Signal | `Payload` discriminant | Silver table |
|--------|----------------------|-------------|
| `Spindle.Speed`, `Temperature.*`, `Vibration.*`, … | `null` | `EquipmentTelemetry` |
| `State` | `{ State, StateReason, OperatorId }` | `EquipmentActual` |
| `WorkRequest` | `{ RequestId, WorkCenterId, … }` | `WorkRequest` |
| `WorkResponse` | `{ ResponseId, RequestId, … }` | `WorkResponse` |
| `MaterialActual` | `{ LotId, Direction, … }` | `MaterialActual` |
| `QualityTest` | `{ TestId, Parameter, MeasuredValue, … }` | `QualityTestResult` |

**Example — Equipment Telemetry:**
```json
{
  "Timestamp": "2026-07-28T11:00:00Z",
  "WorkUnitId": "wu-lyon-prod-tour1",
  "Signal": "Temperature.Spindle",
  "Value": 62.4,
  "Payload": null
}
```

**Example — Quality Test Result:**
```json
{
  "Timestamp": "2026-07-28T11:15:00Z",
  "WorkUnitId": "wu-lyon-qual-cmm1",
  "Signal": "QualityTest",
  "Value": 49.97,
  "Payload": {
    "TestId": "QT-20260728-00042",
    "WorkUnitId": "wu-lyon-qual-cmm1",
    "ResponseId": "WRS-20260728-0003",
    "LotId": "LOT-20260728-0007",
    "TestSpecificationId": "SPEC-CRANK7B-DIAM",
    "Parameter": "Diameter.Main",
    "MeasuredValue": 49.97,
    "LowerLimit": 49.75,
    "UpperLimit": 50.00,
    "UnitOfMeasure": "mm",
    "Result": "Pass",
    "Severity": "None"
  }
}
```

## Demo Scenarios

Set the `DEMO_SCENARIO` environment variable before running:

| Value | What happens | Agent triggered |
|-------|-------------|----------------|
| `Normal` | Healthy baseline, ~95% OEE, 0–4% scrap | — |
| `TemperatureDrift` | Spindle temp on `wu-lyon-prod-tour1` drifts +0.5%/tick (max +30%) | **Maintenance** |
| `QualityExcursion` | Scrap rate on `PROD-CRANK-7B` jumps to 10–25% | **Quality** |
| `MachineFault` | `wu-lyon-crank-centre1` forced to `Fault` after 5 ticks | **Operations + Plant Manager** |
| `ShiftChange` | All WorkUnits transition `Idle → Active` over 2 ticks | — |

## Timers

| Function | CRON | Data emitted |
|----------|------|-------------|
| `TelemetryTick` | every 10 s | Equipment signals (all WorkUnits) + state change events |
| `WorkOrderTick` | every 60 s | Work orders, material actuals, quality test results |

## Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- Azure IoT Hub with a registered device (see `infra/`)
- [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) for local storage emulation

### Run locally

```bash
# 1. Copy the settings template
cp local.settings.json.sample local.settings.json

# 2. Fill in your IoT Hub device connection string
#    Edit: IoTHubDeviceConnectionString

# 3. (Optional) Set the demo scenario
#    Edit: "DEMO_SCENARIO": "TemperatureDrift"

# 4. Start Azurite in a separate terminal
azurite --silent

# 5. Run
func start
```

### Deploy to Azure

```bash
func azure functionapp publish <your-function-app-name>
```

Set `IoTHubDeviceConnectionString` and `DEMO_SCENARIO` in the Function App application settings.
