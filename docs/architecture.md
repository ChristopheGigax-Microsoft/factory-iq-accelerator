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

## Engine Topology

Each engine mirrors these logical modules:

- `capacity`
- `workspace`
- `eventhouse`
- `eventstream`

## Contract-Driven Integration

Deployment outputs a single artifact `connection.json`.
Model deployment consumes only this contract.

## Optional Fabric App and SQL Baseline Path

- `src/fabric-apps` hosts the Rayfin workspace app for online baseline management.
- When `connection.json` contains `sqlBaseline` metadata, baseline seed writes target SQL tables in `shared/sql/baseline/`.
- When SQL metadata is absent, the existing Eventhouse/KQL hierarchy seed behavior remains available as fallback.

## Cross-Engine Parity Validation Checklist

- Use equivalent input values (`plant_code`, `environment`, `region`, `capacity_sku`) for each engine.
- Validate each engine emits a `connection.json` with all required contract fields.
- Confirm naming outputs follow `fiq-{plant_code}-{environment}-{resource}` pattern.
- Confirm deployed logical resources are equivalent: capacity, workspace, eventhouse, KQL database, eventstream.
- Run model deployment twice for each engine output and verify second run is a no-op.

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
2. **Fabric IQ**: Semantic layer for ontology-grounded queries (shared entity model)

### RBAC
All role assignments are provisioned via IaC (both Terraform and Bicep):
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
