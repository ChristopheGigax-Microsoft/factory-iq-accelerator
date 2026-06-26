# Quickstart - Fabric Apps ISA-95 Baseline Management

## Goal
Validate end-to-end capability for online ISA-95 baseline management with SQL persistence and SQL-targeted initial seed flow.

## Prerequisites
- Azure CLI authenticated (`az login`) and subscription selected.
- Existing factory foundation deployed (workspace/eventhouse/eventstream) via selected IaC engine.
- SQL database provisioned for baseline persistence in target environment.
- Python 3.11+ available for runner and validation scripts.

## 1. Confirm feature branch artifacts
- Review planning artifacts in `specs/002-add-fabric-apps-sql/`.
- Confirm contracts in `specs/002-add-fabric-apps-sql/contracts/`.

## 2. Prepare connection handoff for SQL-aware workflows
- Start from current `connection.json` output contract.
- Add SQL metadata as optional additive fields (non-breaking).
- Keep required v1.0 fields unchanged.

## 3. Validate hierarchy input
```bash
python shared/scripts/seed-hierarchy.py --config shared/isa95-model/config/plant-hierarchy.yaml
```

## 4. Run baseline deployment with SQL target behavior
- Use the updated model deployment workflow that writes baseline seed data to SQL instead of Eventhouse.
- Capture logs and seed run metadata for validation.

```bash
python shared/scripts/deploy-model.py \
	--connection ./connection.json \
	--core-dir ./shared/isa95-model/core \
	--extensions-dir ./shared/isa95-model/extensions \
	--hierarchy-config ./shared/isa95-model/config/plant-hierarchy.yaml
```

## 5. Verify outcomes
- SQL contains seeded ISA-95 hierarchy entities.
- Re-running seed operation does not create duplicate logical entities.
- Baseline change records and seed run status are queryable.
- Eventhouse is no longer baseline write target for initial seed workflow.

```sql
SELECT COUNT(*) AS baseline_nodes FROM dbo.isa95_baseline_node;
SELECT TOP 5 seed_run_id, status, started_at, completed_at FROM dbo.baseline_seed_run ORDER BY started_at DESC;
```

## 6. Smoke test online management
- Bootstrap app from Rayfin SDK template in `src/fabric-apps/` using the pinned official command.
- Launch Fabric app (Rayfin SDK-based) in workspace context.
- Execute baseline read and update scenarios.
- Confirm persisted changes are visible after reload and include audit attribution.

### Rayfin Bootstrap Command (Pin Before Implementation)
- Official command from `microsoft/rayfin` README:
```bash
npm create @microsoft/rayfin@latest
```
- Run it from `src/fabric-apps/`.
- Recommended follow-up command from the same README to deploy/run:
```bash
npx rayfin up
```

## 7. Rollback/compatibility check
- Validate legacy consumers still parse required `connection.json` fields.
- Confirm unchanged flows that rely on existing contract fields continue to function.

## 8. Success Criteria Measurement Gates

- SC-001: Record task completion times for baseline create/update journey and compute 95th percentile result.
- SC-003: Track seed run outcomes across fresh environment runs and calculate success percentage.
- SC-005: Run usability review on validation error messages and record actionable clarity outcomes.

### SC-001 Measurement Template

| Run | Start (UTC) | End (UTC) | Duration (sec) | User | Result |
|---|---|---|---|---|---|
| 1 |  |  |  |  |  |

### SC-003 Seed Reliability Log

| Run Date | Environment | Result | Seed Run Id | Notes |
|---|---|---|---|---|
|  |  |  |  |  |

### SC-005 Validation Error Review

| Scenario | Error Message | User Action Clear? (Y/N) | Follow-up Needed |
|---|---|---|---|
| Missing parent node |  |  |  |
