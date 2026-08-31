# OPC UA Data Generator

Demo data generator for the **Factory IQ Accelerator**. Simulates the same ISA-95
demo plant as `samples/isa-95-data-generator` (Usine Lyon), but exposes it as a
**live OPC UA server** instead of pushing messages to Azure IoT Hub.

This is the sample to run when demonstrating **Foundry Local / edge deployment**:
it plugs directly into `OpcUaMachineDataTool.cs`
(`src/foundry-agents/shared/FactoryIQ.Agents.Shared/Local/Tools/OpcUa/`) as a
realistic OPC UA source a manufacturing agent can query — signals, equipment
state, and alarms — without any Azure dependency.

> This project intentionally does **not** reference `samples/isa-95-data-generator`.
> The ISA-95 plant model, scenarios, and simulation logic are copied and adapted
> here on purpose so each sample stays a self-contained artifact that can be
> demoed, forked, or deployed independently.

## Architecture

```
OPC UA Data Generator (console app)
  ├─ ScenarioController        (DEMO_SCENARIO env var)
  ├─ TelemetryGenerator        (per-signal values, anomalies)
  ├─ MachineStateGenerator     (equipment state machine + alarms)
  └─ FactoryOpcUaServer        (OPC Foundation .NET Standard stack)
        └─ FactoryNodeManager  (ISA-95 topology → OPC UA address space)
              └─ opc.tcp://localhost:4855/FactoryIQ/OpcUaDataGenerator
```

## ISA-95 Topology (Usine Lyon)

Same plant as the ISA-95/IoT Hub generator: 3 areas (Production Moteurs,
Contrôle Qualité, Usinage Vilebrequins), 7 WorkUnits (CNC lathes, grinder, CMM,
test bench). See `samples/isa-95-data-generator/README.md` for the full
topology diagram.

## OPC UA Address Space

```
Objects
 └─ FactoryIQ
      └─ site-lyon
           ├─ area-lyon-production
           │    └─ wc-lyon-prod-01
           │         ├─ wu-lyon-prod-tour1
           │         │    ├─ State                  (UInt32 — 0=Active,1=Idle,2=Held,3=Fault,4=Setup)
           │         │    ├─ ActiveAlarmCode         (String, empty when healthy)
           │         │    ├─ ActiveAlarmSeverity     (String)
           │         │    ├─ Spindle.Speed           (Double, rpm)
           │         │    ├─ Temperature.Spindle     (Double, °C)
           │         │    ├─ Vibration.Velocity      (Double, mm/s)
           │         │    ├─ CuttingForce            (Double, N)
           │         │    ├─ FeedRate                (Double, mm/min)
           │         │    └─ Coolant.FlowRate        (Double, L/min)
           │         ├─ wu-lyon-prod-tour2  (same signal set)
           │         └─ wu-lyon-prod-rect1  (grinder signal set)
           ├─ area-lyon-quality
           │    └─ wc-lyon-qual-01
           │         ├─ wu-lyon-qual-cmm1    (CMM signal set)
           │         └─ wu-lyon-qual-bench1  (test bench signal set)
           └─ area-lyon-crankshaft
                └─ wc-lyon-crank-01
                     ├─ wu-lyon-crank-centre1
                     └─ wu-lyon-crank-tour1
```

Node IDs follow the pattern `{WorkUnitId}.{Signal}`, e.g.
`wu-lyon-prod-tour1.Temperature.Spindle`, in namespace
`http://factoryiq.local/opcua/`.

## Demo Scenarios

Set the `DEMO_SCENARIO` environment variable before running (same scenarios as
the ISA-95/IoT Hub generator):

| Value | What happens | Agent triggered |
|-------|-------------|----------------|
| `Normal` | Healthy baseline, ~95% OEE, 0–4% scrap | — |
| `TemperatureDrift` | Spindle temp on `wu-lyon-prod-tour1` drifts +0.5%/tick (max +30%) | **Maintenance** |
| `QualityExcursion` | Scrap rate on `PROD-CRANK-7B` jumps to 10–25% | **Quality** |
| `MachineFault` | `wu-lyon-crank-centre1` forced to `Fault` after 5 ticks — raises an OPC UA alarm | **Operations + Plant Manager** |
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
security policy `None`, browse to `Objects → FactoryIQ → site-lyon → ...`, and
subscribe to a signal node to watch it update every 10 seconds.

### Connect it to a Foundry Local agent

This generator is the intended data source for
`OpcUaMachineDataTool.cs`
(`src/foundry-agents/shared/FactoryIQ.Agents.Shared/Local/Tools/OpcUa/`).
Implement that tool with an OPC UA client (e.g.
`OPCFoundation.NetStandard.Opc.Ua.Client`) pointed at
`opc.tcp://localhost:4855/FactoryIQ/OpcUaDataGenerator`, then run a Factory IQ
agent in local mode (see `docs/foundry-local.md`) to query live equipment
state, alarms, and telemetry — entirely offline once the model is cached.
