# Implementation Plan: Fabric Apps ISA-95 Baseline Management

**Branch**: `[002-add-fabric-apps-sql]` | **Date**: 2026-06-26 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-add-fabric-apps-sql/spec.md`

## Summary

Add a Fabric workspace app (Rayfin SDK-based experience) for online ISA-95 baseline management, introduce SQL as the baseline system of record for this phase, and update initial baseline deployment behavior so seed writes target SQL instead of Eventhouse while preserving backward-compatible handoff contracts.

## Technical Context

**Language/Version**: Python 3.11+ (runner/scripts), SQL (schema and seed logic), TypeScript (Fabric app/Rayfin SDK surface)

**Primary Dependencies**: Azure CLI auth context, PyYAML (existing), SQL client library for Python runner path, Rayfin SDK app runtime

**Storage**: SQL database for baseline persistence (authoritative for this feature phase); Eventhouse remains for analytical/event scenarios outside baseline CRUD

**Testing**: Script validation and smoke reruns for idempotency, contract validation checks, integration checks for app->SQL read/write and concurrency conflicts

**Target Platform**: Azure-hosted factory foundation with Microsoft Fabric workspace context

**Project Type**: IaC accelerator + model runner scripts + workspace app integration

**Performance Goals**:
- 95% of baseline create/update operations completed by users in under 3 minutes (SC-001)
- 99% seed success in fresh environments (SC-003)

**Constraints**:
- Maintain connection contract backward compatibility (required v1.0 fields unchanged)
- No secrets in source control; use identity-based access patterns
- Seed flow must be idempotent and rerunnable without duplicate logical entities
- Shared ISA-95 model remains single source of truth under `shared/`

**Scale/Scope**:
- Single-plant workspace context per deployment
- Baseline hierarchy and associated operational baseline records
- Initial scope includes app behavior, SQL persistence, and seed target migration only

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Phase 0 Gate Assessment

1. **Principle 1 - Engine Independence**: PASS (feature will require equivalent SQL-baseline support design for each supported engine path in this repo, without mixing engines in one deployment).
2. **Principle 2 - Shared Model SSoT**: PASS (shared ISA-95 assets remain under `shared/` and are reused).
3. **Principle 3 - Output Contract Sacred**: PASS (contract evolution is additive and backward-compatible; required fields preserved).
4. **Principle 4 - ISA-95 Conformance**: PASS (hierarchy and operational model remain ISA-95 aligned).
5. **Principle 5 - Idempotency by Construction**: PASS (SQL target seed flow explicitly requires rerun-safe writes and atomic behavior).
6. **Principle 6 - Customizability Without Forking**: PASS (core vs extension boundaries remain intact).
7. **Principle 7 - Start Small, Expand by Parameter**: PASS (v1 scope in constitution now explicitly permits optional Fabric app + SQL baseline management when selected by feature scope).

### Post-Phase 1 Re-Check

1. Principles 1-6 remain satisfied by the generated design artifacts.
2. Principle 7 scope alignment is satisfied under constitution v1.1.0.
3. No unresolved constitutional exceptions remain for this feature plan.

## Project Structure

### Documentation (this feature)

```text
specs/002-add-fabric-apps-sql/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── fabric-baseline-app-interface.md
│   └── sql-seed-runner-contract.md
└── tasks.md            # Created later by /speckit.tasks
```

### Source Code (repository root)

```text
shared/
├── isa95-model/
│   ├── config/
│   ├── core/
│   └── extensions/
└── scripts/
    ├── deploy-model.py        # To be updated for SQL baseline target behavior
    └── seed-hierarchy.py       # Reused for hierarchy validation inputs

src/
└── fabric-apps/
    ├── src/                    # Rayfin SDK bootstrapped workspace app code
    └── rayfin/                 # Fabric app metadata and schema

infra/
├── bicep/
│   └── modules/                # SQL-related provisioning integration aligned with engine model
└── terraform/
    └── modules/                # SQL-related provisioning integration aligned with engine model

contracts/
├── connection-contract.md      # Backward-compatible additive evolution only
└── model-runner-interface.md
```

**Structure Decision**: Keep IaC concerns in `infra/` and place Fabric app code in `src/fabric-apps/`, bootstrapped via Rayfin SDK and extended in a single app project without a separate API service folder.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
