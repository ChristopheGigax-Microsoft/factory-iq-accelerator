# Fabric Ontology for Factory IQ (IaC Implementation)

This document describes how the accelerator implements Fabric Ontology in infrastructure code while preserving the integration constraint:

- Foundry stays connected to the **Fabric Data Agent** endpoint only.
- Ontology is attached **inside** the Data Agent as a second source.

## Integration contract

The runtime path is:

**Foundry Agent** -> `fabric_iq_preview` project connection -> **Fabric Data Agent MCP endpoint** -> (KQL source + Ontology source)

No direct Foundry connection to an ontology endpoint is created.

## Terraform implementation

Terraform now provisions:

1. `fabric_ontology` resource (`infra/terraform/modules/ontology`), with:
   - ontology definition root (`definition.json`)
   - core ISA-95 operations entity types
   - relationship types
   - Kusto data bindings to Eventhouse tables
2. Eventhouse Silver physical model (`infra/terraform/modules/eventhouse/definitions/silver_model.kql`) with:
   - table creation (`.create-merge`) for ISA-95 operational facts
   - update policies (`.alter table ... policy update`) from `TelemetryLanding`
3. `fabric_data_agent` definition includes:
   - existing KQL datasource
   - ontology datasource (`type: ontology`) bound to the newly created ontology item
4. Connection contract output includes:
   - `fabricOntologyId`
   - `fabricOntologyName`

Implementation files:

- `infra/terraform/modules/ontology/*`
- `infra/terraform/modules/eventhouse/definitions/silver_model.kql`
- `infra/terraform/modules/data_agent/definitions/datasource_ontology.json.tmpl`
- `infra/terraform/main.tf`
- `infra/terraform/outputs.tf`

## Ontology scope used by the accelerator

The deployed ontology models manufacturing operations semantics centered on:

- `Enterprise`
- `Site`
- `Area`
- `WorkCenter`
- `WorkUnit`
- `WorkRequest`
- `WorkResponse`
- `QualityTest`

with relationships:

- `containsSite` (Enterprise -> Site)
- `containsArea` (Site -> Area)
- `containsWorkCenter` (Area -> WorkCenter)
- `containsWorkUnit` (WorkCenter -> WorkUnit)
- `fulfillsRequest` (WorkResponse -> WorkRequest)
- `validatesResponse` (QualityTest -> WorkResponse)

Planned Kusto data bindings (not yet active in Terraform because Fabric Ontology import currently fails with `ALMOperationImportFailed` when DataBindings parts are included):

- `WorkRequest` -> `WorkRequest`
- `WorkResponse` -> `WorkResponse`
- `QualityTest` -> `QualityTestResult`

The broader recommended ISA-95 model blueprint remains in:

- `shared/ontology/factory-iq-ontology-blueprint.yaml`

## Data Agent behavior alignment

Data Agent instructions are ontology-aware and enforce this strategy:

- Use ontology for business semantics/KPI meaning.
- Use KQL for operational diagnostics and time-series evidence.
- Keep responses grounded and actionable.

## Bronze -> Silver update policies (detailed behavior)

The KQL script `infra/terraform/modules/eventhouse/definitions/silver_model.kql` defines update policies that run automatically when new rows are ingested into `TelemetryLanding`.

### 1. `EquipmentTelemetry`
- **Source**: `TelemetryLanding`
- **Filter**: rows with `Timestamp`, `WorkUnitId`, and `Signal` populated
- **Projection**: `Timestamp`, `WorkUnitId`, `Signal`, `Value`
- **Purpose**: normalized time-series telemetry for trend charts and anomaly detection

### 2. `EquipmentActual`
- **Source**: `TelemetryLanding`
- **Filter**: rows where `Payload.State` exists
- **Projection**:
  - `Timestamp` = `Payload.Timestamp` fallback to landing `Timestamp`
  - `WorkUnitId` = `Payload.WorkUnitId` fallback to landing `WorkUnitId`
  - `State`, `StateReason`, `OperatorId` from payload
- **Purpose**: machine state/event history (running, fault, held, etc.)

### 3. `WorkRequest`
- **Source**: `TelemetryLanding`
- **Filter**: rows where `Payload.RequestId` exists
- **Projection**: request/order metadata (`RequestId`, `WorkCenterId`, `ProductId`, quantities, schedule, status)
- **Purpose**: production order intent layer (planned work)

### 4. `WorkResponse`
- **Source**: `TelemetryLanding`
- **Filter**: rows where `Payload.ResponseId` exists
- **Projection**: execution outcomes (`ResponseId`, `RequestId`, actual times, produced/rejected quantities, status)
- **Purpose**: realized production results (actual work completion)

### 5. `MaterialActual`
- **Source**: `TelemetryLanding`
- **Filter**: rows where both `Payload.LotId` and `Payload.Direction` exist
- **Projection**: lot/material movement fields (lot, material, work center, quantity, UoM, direction)
- **Purpose**: traceability of consumed/produced material flows

### 6. `QualityTestResult`
- **Source**: `TelemetryLanding`
- **Filter**: rows where `Payload.TestId` exists
- **Projection**: quality inspection attributes (`TestId`, `ResponseId`, limits, measured value, result, severity)
- **Purpose**: defect/scrap and quality outcome analysis

## Operational implications

- Policies are **idempotent to deploy** (`.alter table ... policy update`), so Terraform can reapply safely.
- Policies are **event-driven on ingestion**: they transform only newly ingested Bronze records.
- If an expected payload field is missing, that specific Silver projection does not get a row for that event.
- Dashboards and querysets are intended to read Silver tables, not `TelemetryLanding` directly.

## Graph model population

- Ontology types (entities/relationships) define the graph schema.
- Data bindings instantiate graph nodes from Silver table rows.
- If bound tables are empty (or key columns are missing), graph nodes/edges remain empty in the workspace UI.
