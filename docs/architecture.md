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
