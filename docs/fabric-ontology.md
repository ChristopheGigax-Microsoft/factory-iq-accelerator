# Fabric Ontology for Factory IQ (Data Agent Path)

This guide defines a practical Fabric Ontology for the Factory IQ Accelerator and explains how to plug it into the **Fabric Data Agent** while keeping Azure AI Foundry connected to the Data Agent endpoint only.

## Target Integration Pattern

- **Foundry agents** keep using `fabric-iq-data-agent-connection`.
- **No direct Foundry connection** to the Ontology MCP endpoint.
- Ontology is consumed *inside* Fabric by the Data Agent as an additional governed source.

Flow:

1. Build/publish Ontology in Fabric.
2. Add Ontology as a source in the Fabric Data Agent.
3. Publish Data Agent.
4. Foundry agents continue querying the same Data Agent MCP endpoint.

## Why this pattern fits the accelerator

- Preserves current IaC + Foundry wiring (`fabric_iq_preview` -> Data Agent MCP).
- Adds semantic grounding (business entities/relations) without changing agent code.
- Keeps governance and permissions centralized in Fabric Data Agent + Purview policy boundary.

## Recommended Factory IQ Ontology (v1)

Use `shared/ontology/factory-iq-ontology-blueprint.yaml` as the starting blueprint.

### Entity types

- `Enterprise`
- `Site`
- `Area`
- `WorkCenter`
- `WorkUnit`
- `WorkRequest`
- `WorkResponse`
- `MaterialLot`
- `QualityTest`
- `EquipmentStateEvent`
- `TelemetrySignal`

### Key relationships

- Enterprise contains Site
- Site contains Area
- Area contains WorkCenter
- WorkCenter contains WorkUnit
- WorkCenter executes WorkRequest
- WorkResponse fulfills WorkRequest
- WorkUnit emits EquipmentStateEvent
- WorkUnit emits TelemetrySignal
- WorkResponse validatesThrough QualityTest
- MaterialLot consumedBy WorkRequest
- MaterialLot producedBy WorkResponse

### KPI concepts (semantic metrics)

- OEE
- Availability
- Performance
- QualityRate
- ScrapRate
- MTTR
- MTBF
- ScheduleAdherence

## Data mapping to current accelerator assets

Current repo assets already provide the data required by this ontology:

- ISA-95 hierarchy baseline (SQL): `dbo.isa95_baseline_node`
- Real-time ops facts (KQL Eventhouse):
  - `EquipmentActual`
  - `EquipmentTelemetry`
  - `WorkRequest`
  - `WorkResponse`
  - `MaterialActual`
  - `QualityTestResult`

## Implementation steps in Fabric

### 1. Create the ontology item

Preferred options:

1. Generate from a semantic model (fastest, easiest governance).
2. Build directly from OneLake/Eventhouse bindings (more control).

For this accelerator, start from a semantic model representing ISA-95 hierarchy + operations facts, then refine entities and relationships using the blueprint.

### 2. Add ontology as a Data Agent source

In the Fabric Data Agent:

1. Open **Add data source**.
2. Add the Ontology item.
3. Keep Eventhouse KQL source enabled (already configured by accelerator).
4. Publish Data Agent.

Notes:

- Ontology source supports description + agent-level instructions.
- Ontology source does not support table selection or few-shot query examples.

### 3. Update Data Agent instructions

Add these routing rules to agent-level instructions:

- Use ontology for business-meaning queries (asset lineage, process context, KPI definitions).
- Use KQL source for deep time-series diagnostics and high-granularity telemetry.
- For aggregation accuracy on ontology queries, include: `Support group by in GQL`.

### 4. Validate through the existing Foundry connection

Do not change Foundry connection targets. Validate via current Data Agent MCP endpoint:

- Hierarchy/context query:
  - "Which work units belong to line-lyon-01 and what quality tests are linked to their latest responses?"
- KPI/diagnostic blend:
  - "Show last 24h OEE for line-paris-01 and explain top state-reason losses."

If both semantic context and telemetry detail are returned, the Data Agent is correctly using ontology + KQL under the same endpoint.

## Operational caveats

- Ontology and Fabric IQ features are preview at the time of writing.
- Data Agent supports up to five total sources.
- Cross-region capacity constraints can affect Data Agent query execution.
- All access remains user-context (OBO) and policy-enforced.
