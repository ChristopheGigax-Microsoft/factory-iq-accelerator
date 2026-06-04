# Data Model — Fabric Foundation Baseline

## Entity: PlantDeploymentContext

Purpose: Input context controlling one-plant deployment naming and placement.

Fields:
- `plantCode` (string, required): lowercase alphanumeric/hyphen plant identifier.
- `environment` (string, required): deployment stage (`dev`, `test`, `prod`, or org-approved equivalent).
- `region` (string, required): Azure region selected by customer.
- `capacitySku` (string, required): default `F2`, parameterized.
- `engine` (enum, required): `terraform` | `bicep` | `pulumi`.

Validation rules:
- `plantCode` and `environment` must satisfy naming regex and resource length budgets.
- `engine` must be exactly one value.
- `region` must be explicitly provided.

State transitions:
- `draft` -> `validated` -> `deployed` -> `verified`.

## Entity: FabricFoundation

Purpose: Logical representation of provisioned baseline resources.

Fields:
- `capacityName` (string)
- `workspaceName` (string)
- `eventhouseName` (string)
- `kqlDatabaseName` (string)
- `eventstreamName` (string)
- `deploymentTimestamp` (datetime)

Relationships:
- One `PlantDeploymentContext` produces one `FabricFoundation`.
- One `FabricFoundation` emits one `ConnectionContract` payload.

Validation rules:
- All names must derive from `plantCode` + `environment` deterministically.
- Resource set must be complete for success state.

State transitions:
- `provisioning` -> `ready` -> `reconciled` (on rerun without drift).

## Entity: ConnectionContract

Purpose: Engine-agnostic handoff artifact used by model runner.

Fields:
- `tenantId` (string)
- `subscriptionId` (string)
- `resourceGroup` (string)
- `region` (string)
- `workspaceId` (string)
- `eventhouseId` (string)
- `kqlDatabase` (string)
- `engineMetadata` (object, optional for diagnostics only; not used by runner logic)

Relationships:
- Produced by `FabricFoundation` deployment.
- Consumed by `ModelDeploymentRun`.

Validation rules:
- Must satisfy schema in feature contract docs.
- Missing required fields fails model run preflight.

State transitions:
- `generated` -> `validated` -> `consumed`.

## Entity: PlantHierarchyConfig

Purpose: Customer-editable ISA-95 structure source.

Fields:
- `enterprises` (array)
- `sites` (array)
- `areas` (array)
- `workCenters` (array)
- `workUnits` (array)

Relationships:
- Used by `ModelDeploymentRun` seeding stage.
- Maps to ISA-95 dimension entities.

Validation rules:
- Required hierarchy levels must be present.
- Parent references must be resolvable.

State transitions:
- `authored` -> `validated` -> `seeded`.

## Entity: ModelDeploymentRun

Purpose: Execution instance applying core + extension schema and hierarchy seeding.

Fields:
- `runId` (string)
- `startedAt` (datetime)
- `completedAt` (datetime)
- `status` (enum: `running`, `succeeded`, `failed`, `no-op`)
- `coreApplied` (boolean)
- `extensionsApplied` (boolean)
- `hierarchySeeded` (boolean)
- `rerunSafe` (boolean)

Relationships:
- Consumes one `ConnectionContract`.
- Consumes one `PlantHierarchyConfig`.
- Produces ISA-95 entities and telemetry ingestion policy in KQL.

Validation rules:
- Core schema must exist before update policies are applied.
- Re-run must not create duplicate schema artifacts.

State transitions:
- `running` -> `succeeded`
- `running` -> `failed`
- `running` -> `no-op` (valid idempotent rerun)

## ISA-95 Core Logical Entities

### Equipment Hierarchy
- `Enterprise`
- `Site`
- `Area`
- `WorkCenter`
- `WorkUnit`

Relationships:
- Strict parent-child chain: Enterprise -> Site -> Area -> WorkCenter -> WorkUnit.

### Operational Facts
- `EquipmentTelemetry`
- `EquipmentState`
- `ProductionEvent`
- `QualityEvent`

Validation rules:
- Telemetry ingestion must route through landing then transform policy.
- Fact records must reference valid hierarchy identifiers where applicable.
