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
2. Eventhouse Bronze physical model
   (`infra/terraform/modules/eventhouse/definitions/bronze_model.kql`) with the
   lossless `RawTelemetry` table.
3. `fabric_data_agent` definition includes:
   - existing KQL datasource
   - ontology datasource (`type: ontology`) bound to the newly created ontology item
4. Connection contract output includes:
   - `fabricOntologyId`
   - `fabricOntologyName`

Implementation files:

- `infra/terraform/modules/ontology/*`
- `infra/terraform/modules/eventhouse/definitions/bronze_model.kql`
- `samples/routing/isa95-demo/routes.json`
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

## Bronze and optional ontology bindings

The accelerator no longer deploys a universal Silver schema. It always deploys
`RawTelemetry`; each customer may provide a routing profile that creates the
tables required by its ontology bindings.

The optional profile under `samples/routing/isa95-demo/` recreates the
`EquipmentTelemetry`, `EquipmentActual`, `WorkRequest`, `WorkResponse`,
`MaterialActual`, and `QualityTestResult` tables for the repository's ISA-95
generator. It is a demonstration profile, not an accelerator contract.

See [Customer routing profiles](routing-profiles.md).

## Operational implications

- Routing policies are event-driven and transform only newly ingested Bronze
  records.
- The default dashboard and queryset read `RawTelemetry`.
- Customer ontology bindings must target tables created by the selected routing
  profile.

## Graph model population

- Ontology types (entities/relationships) define the graph schema.
- Data bindings instantiate graph nodes from Silver table rows.
- If bound tables are empty (or key columns are missing), graph nodes/edges remain empty in the workspace UI.
