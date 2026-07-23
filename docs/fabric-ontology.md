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
2. `fabric_data_agent` definition includes:
   - existing KQL datasource
   - ontology datasource (`type: ontology`) bound to the newly created ontology item
3. Connection contract output includes:
   - `fabricOntologyId`
   - `fabricOntologyName`

Implementation files:

- `infra/terraform/modules/ontology/*`
- `infra/terraform/modules/data_agent/definitions/datasource_ontology.json.tmpl`
- `infra/terraform/main.tf`
- `infra/terraform/outputs.tf`

## Ontology scope used by the accelerator

The deployed ontology models manufacturing operations semantics centered on:

- `WorkRequest`
- `WorkResponse`
- `QualityTest`

with relationships:

- `fulfillsRequest` (WorkResponse -> WorkRequest)
- `validatesResponse` (QualityTest -> WorkResponse)

and Kusto data bindings against:

- `WorkRequest`
- `WorkResponse`
- `QualityTestResult`

The broader recommended ISA-95 model blueprint remains in:

- `shared/ontology/factory-iq-ontology-blueprint.yaml`

## Data Agent behavior alignment

Data Agent instructions are ontology-aware and enforce this strategy:

- Use ontology for business semantics/KPI meaning.
- Use KQL for operational diagnostics and time-series evidence.
- Keep responses grounded and actionable.
