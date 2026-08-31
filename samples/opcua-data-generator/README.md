# OPC UA Line Data Generator

Demo data generator for the **Factory IQ Accelerator**. It simulates a single
production-line OPC UA server — **Lyon Motor Line 1** — instead of a whole plant.
That better matches how an IT/OT team usually exposes machine, cell, or line
controller data into SCADA and edge applications.

This is the sample to run when demonstrating **Foundry Local / edge deployment**:
it plugs directly into `OpcUaMachineDataTool.cs`
(`src/foundry-agents/shared/FactoryIQ.Agents.Shared/Local/Tools/OpcUa/`) as a
realistic line-level OPC UA source a manufacturing agent can query — signals,
equipment state, and alarms — without any Azure dependency.

> This project intentionally does **not** reference `samples/isa-95-data-generator`.
> The ISA-95 line model, scenarios, and simulation logic are copied and adapted
> here on purpose so each sample stays a self-contained artifact that can be
> demoed, forked, or deployed independently.

## Architecture

```
OPC UA Line Data Generator (console app)
  ├─ ScenarioController        (DEMO_SCENARIO env var)
  ├─ TelemetryGenerator        (per-signal values, anomalies)
  ├─ MachineStateGenerator     (equipment state machine + alarms)
  └─ FactoryOpcUaServer        (OPC Foundation .NET Standard stack)
        └─ FactoryNodeManager  (one line topology → OPC UA address space)
              └─ opc.tcp://localhost:4855/FactoryIQ/OpcUaDataGenerator
```

## Line topology

The server represents **one edge OPC UA endpoint for one production line**:

- Site/gateway: `site-lyon-edge` — Lyon Edge Gateway
- Area: `area-lyon-motor-line` — Lyon Motor Line 1
- Line controller: `line-lyon-motor-01`
- Stations/work units:
  - `wu-lyon-prod-tour1` — CNC Lathe #1
  - `wu-lyon-prod-tour2` — CNC Lathe #2
  - `wu-lyon-prod-rect1` — Crankshaft Grinder
  - `wu-lyon-qual-cmm1` — Inline CMM Station
  - `wu-lyon-qual-bench1` — End-of-Line Test Rig

## OPC UA Address Space

```
Objects
 └─ FactoryIQ
      └─ site-lyon-edge
           └─ area-lyon-motor-line
                └─ line-lyon-motor-01
                     ├─ wu-lyon-prod-tour1
                     │    ├─ State                  (UInt32 — 0=Active,1=Idle,2=Held,3=Fault,4=Setup)
                     │    ├─ ActiveAlarmCode         (String, empty when healthy)
                     │    ├─ ActiveAlarmSeverity     (String)
                     │    ├─ Spindle.Speed           (Double, rpm)
                     │    ├─ Temperature.Spindle     (Double, degC)
                     │    ├─ Vibration.Velocity      (Double, mm/s)
                     │    ├─ CuttingForce            (Double, N)
                     │    ├─ FeedRate                (Double, mm/min)
                     │    └─ Coolant.FlowRate        (Double, L/min)
                     ├─ wu-lyon-prod-tour2  (same CNC signal set)
                     ├─ wu-lyon-prod-rect1  (grinder signal set)
                     ├─ wu-lyon-qual-cmm1   (inline CMM signal set)
                     └─ wu-lyon-qual-bench1 (end-of-line test rig signal set)
```

Node IDs follow the pattern `{WorkUnitId}.{Signal}`, e.g.
`wu-lyon-prod-tour1.Temperature.Spindle`, in namespace
`http://factoryiq.local/opcua/`.

## Demo Scenarios

Set the `DEMO_SCENARIO` environment variable before running (same scenarios as
the ISA-95/IoT Hub generator):

| Value | What happens | Agent triggered |
|-------|-------------|----------------|
| `Normal` | Healthy line baseline, ~95% OEE, 0–4% scrap | — |
| `TemperatureDrift` | Spindle temp on `wu-lyon-prod-tour1` drifts +0.5%/tick (max +30%) | **Maintenance** |
| `QualityExcursion` | Scrap rate on `PROD-ENGINE-7B` jumps to 10–25% | **Quality** |
| `MachineFault` | `wu-lyon-prod-tour2` forced to `Fault` after 5 ticks — raises an OPC UA alarm | **Operations + Plant Manager** |
| `ShiftChange` | All WorkUnits transition `Idle → Active` over 2 ticks | — |

Ticks run every 10 seconds (equipment telemetry + state/alarm transitions).

## Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- No Azure resources required — this generator is fully self-contained.

### Run locally

```powershell
# (Optional) Set the demo scenario
$env:DEMO_SCENARIO = "TemperatureDrift"

cd samples/opcua-data-generator/src
dotnet run
```

On first run the OPC Foundation stack generates a self-signed application
certificate under `%LocalApplicationData%/FactoryIQ/OpcUaDataGenerator/pki/`.
The server accepts anonymous, unencrypted connections (`SecurityMode: None`)
by default — sufficient for a local demo, **not** for a production deployment.

The server listens at:

```
opc.tcp://localhost:4855/FactoryIQ/OpcUaDataGenerator
```

### Browse it

Any OPC UA client works, e.g. [UaExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html)
or the sample clients bundled with the OPC Foundation SDK. Connect with
security policy `None`, browse to `Objects → FactoryIQ → site-lyon-edge → area-lyon-motor-line → line-lyon-motor-01`, and
subscribe to a signal node to watch it update every 10 seconds.

### Connect it to a Foundry Local agent

This generator is the intended data source for
`OpcUaMachineDataTool.cs`
(`src/foundry-agents/shared/FactoryIQ.Agents.Shared/Local/Tools/OpcUa/`).
The local agents already include an OPC UA client implementation pointed at
`opc.tcp://localhost:4855/FactoryIQ/OpcUaDataGenerator`. Run the generator and
then run the portal or individual Factory IQ agents in local mode (see
`docs/foundry-local.md`) to query live line equipment state, alarms, and
telemetry — entirely offline once the model is cached.
