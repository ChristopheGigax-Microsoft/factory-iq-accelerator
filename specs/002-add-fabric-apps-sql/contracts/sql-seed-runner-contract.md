# Contract: SQL Seed Runner Interface

## Purpose
Defines behavior for the baseline seed/deploy runner when SQL is the initial baseline write target.

## CLI Inputs
- `--connection` (required): path to connection handoff JSON.
- `--hierarchy-config` (required): ISA-95 hierarchy YAML.
- `--core-dir` and `--extensions-dir` (optional for this flow, preserved for compatibility where needed).
- `--fail-on-warning` (optional): strict mode.

## Connection Contract Expectations
- Required existing fields remain mandatory (`tenantId`, `subscriptionId`, `resourceGroup`, `region`, `workspaceId`, `eventhouseId`, `kqlDatabase`, `generatedAt`, `schemaVersion`).
- SQL target metadata is additive/optional and must not break consumers that only rely on required v1.0 fields.

## Stage Order
1. Validate connection contract and hierarchy config.
2. Resolve SQL target metadata from connection.
3. Start seed run record (`BaselineSeedRun` -> Running).
4. Apply idempotent seed writes for hierarchy entities.
5. Persist change/audit records.
6. Mark seed run Succeeded or Failed.
7. If SQL metadata is absent, fall back to existing Eventhouse/KQL hierarchy seed behavior.

## Behavior Guarantees
- Idempotent rerun: repeated execution must not create duplicate logical baseline entities.
- Atomicity: partial failed writes must not leave broken hierarchy state.
- Traceability: every successful mutation is linked to actor/timestamp and optional seed run id.

## Exit Codes
- `0`: success, including convergent rerun.
- Non-zero: validation, connectivity, or persistence failure.

## Failure Expectations
- Invalid parent links must fail with explicit validation diagnostics.
- SQL connectivity/auth failures must be surfaced with actionable error context.
- Failed runs must preserve enough metadata for triage in `BaselineSeedRun`.
