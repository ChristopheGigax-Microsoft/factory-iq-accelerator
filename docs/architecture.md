# Factory IQ Accelerator Architecture

## Canonical Naming Convention

All resources derive from:

- `plant_code`
- `environment`

Canonical pattern:

`fiq-{plant_code}-{environment}-{resource}`

Examples:

- `fiq-plant1-dev-cap`
- `fiq-plant1-dev-ws`
- `fiq-plant1-dev-eh`
- `fiq-plant1-dev-es`

## Naming Constraints

- Lowercase letters, numbers, hyphens only.
- No leading/trailing hyphen.
- Keep each generated name within engine/resource-specific length limits.
- Use deterministic suffixes per resource type.

## Terraform Topology

Terraform provisions these logical modules:

- `capacity`
- `workspace`
- `eventhouse`
- `eventstream`

### Eventhouse Bronze/Silver model

The Eventhouse module provisions and maintains:

- **Bronze** ingestion table: `TelemetryLanding`
- **Silver** operational tables: `EquipmentTelemetry`, `EquipmentActual`, `WorkRequest`, `WorkResponse`, `MaterialActual`, `QualityTestResult`
- **Update policies** from `TelemetryLanding` to each Silver table for automatic projection when matching payload fields exist.

Real-time dashboard/queryset assets are intentionally wired to the Silver tables.

## Contract-Driven Integration

Deployment outputs a single artifact `connection.json`.
Model deployment consumes only this contract.

## Optional Fabric App and SQL Baseline Path

- `src/fabric-apps` hosts the Rayfin workspace app for online baseline management.
- When `connection.json` contains `sqlBaseline` metadata, baseline seed writes target SQL tables in `shared/sql/baseline/`.
- When SQL metadata is absent, the existing Eventhouse/KQL hierarchy seed behavior remains available as fallback.

## Deployment Validation Checklist

- Use expected input values (`plant_code`, `environment`, `region`, `capacity_sku`) for Terraform.
- Validate Terraform emits a `connection.json` with all required contract fields.
- Confirm naming outputs follow `fiq-{plant_code}-{environment}-{resource}` pattern.
- Confirm deployed logical resources are present: capacity, workspace, eventhouse, KQL database, eventstream.
- Run model deployment twice from the same contract and verify second run is a no-op.

## Security Baselines

- No secrets in repository.
- Use Service Principal or Managed Identity.
- Keep state and temporary artifacts out of source control.

## Foundry Agent Layer

The accelerator includes an Azure AI Foundry project with 5 manufacturing agents built on Microsoft Agent Framework (.NET 10).

### Foundry Resources
- AI Foundry Hub + Project (with system-assigned managed identity)
- Azure OpenAI Service (GPT-4o deployment)
- Azure AI Search (semantic search for Foundry IQ knowledge base)
- Storage Account (knowledge base documents in `knowledge-base` container)

### Fabric ↔ Foundry Integration Paths
1. **Fabric Data Agent**: Conversational KQL queries over Eventhouse telemetry (OBO auth)
2. **Fabric Ontology (via Data Agent)**: Semantic layer for ontology-grounded queries remains attached to Data Agent, not directly to Foundry

### RBAC
All role assignments are provisioned via Terraform IaC:
- Foundry Project MI → AI Search (reader + contributor)
- Foundry Project MI → Storage (blob reader)
- Foundry Project MI → OpenAI (user)
- AI Search MI → Storage (blob reader for indexer)
- Hub MI → AI Search (reader) + Storage (contributor)

### Agent Naming
Agents follow the same `fiq-{plant_code}-{environment}` convention:
- `fiq-{plant}-{env}-ai-hub`
- `fiq-{plant}-{env}-ai-project`
- `fiq-{plant}-{env}-openai`
- `fiq-{plant}-{env}-search`

See `docs/foundry-agents.md` for full agent documentation.
