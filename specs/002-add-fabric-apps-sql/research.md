# Phase 0 Research - Fabric Apps ISA-95 Baseline Management

## Decision 1: App architecture uses layered Fabric App + API + SQL persistence
- Decision: Implement the feature as a Fabric workspace app (Rayfin SDK UI) backed by a thin API/business layer and a SQL baseline store.
- Rationale: This keeps UI concerns separate from validation/persistence logic, supports future backend changes, and provides a clean boundary for audit and authorization.
- Alternatives considered:
  - Direct UI-to-database access: rejected due to weak security and maintainability boundaries.
  - Eventhouse as CRUD persistence: rejected because this feature explicitly requires SQL as the operational baseline store.

## Decision 2: Authentication uses managed identity + user attribution
- Decision: Use managed identity for service-to-SQL access, while recording end-user identity for baseline change attribution.
- Rationale: Satisfies the no-secrets constraint, aligns with Azure operational patterns, and supports FR-005 traceability.
- Alternatives considered:
  - SQL credentials/connection string secrets: rejected due to rotation risk and policy misalignment.
  - Service principal secrets only: rejected as default; acceptable only as constrained fallback.

## Decision 3: SQL seeding is idempotent via upsert semantics and transactional execution
- Decision: Replace baseline write target from Eventhouse to SQL using idempotent upsert behavior and explicit transaction boundaries.
- Rationale: Preserves repeatable reruns (FR-008), avoids duplicate logical entities, and prevents partial hierarchy corruption.
- Alternatives considered:
  - Destructive clear-and-reinsert: rejected due to integrity and operational risk.
  - Per-row non-transactional writes: rejected because it can leave partial seeded state.

## Decision 4: Keep connection contract backward compatible by additive evolution
- Decision: Preserve required `connection.json` v1.0 fields and add SQL-related metadata as optional additive fields consumed by SQL-aware workflows.
- Rationale: Respects Constitution Principle 3 (sacred output contract) while enabling this feature without breaking existing model runner behavior.
- Alternatives considered:
  - Immediate breaking schema version change: rejected due to migration and compatibility impact.
  - Separate SQL-only contract file: rejected because it fragments the handoff model.

## Decision 5: Validation is defense-in-depth (app validation + database constraints)
- Decision: Validate ISA-95 hierarchy links before write in app/API workflow and enforce relational integrity constraints in SQL.
- Rationale: Gives clear user feedback while guaranteeing persistence-level data integrity.
- Alternatives considered:
  - App-only validation: rejected (integrity gap).
  - DB-only validation: rejected (poor UX/error clarity).

## Decision 6: Feature rollout remains parameterized and non-destructive
- Decision: Introduce SQL baseline and app capability through engine-aligned parameters and optional fields, without removing existing Eventhouse analytical path in this phase.
- Rationale: Enables gradual rollout and rollback safety, and keeps the accelerator extensible.
- Alternatives considered:
  - Big-bang replacement of existing flows: rejected as high-risk.

## Decision 7: Keep feature scope focused for this increment
- Decision: Keep implementation scope focused on the currently targeted engine paths for this feature increment.
- Rationale: Focused scope reduces delivery risk and accelerates incremental validation.
- Alternatives considered:
  - Expand engine scope in this increment: rejected due to delivery focus and implementation complexity.

## Clarification Resolution Summary
- All technical context clarifications are resolved for planning:
  - Runtime and language choices identified (Python runner updates, TypeScript app surface, SQL schema).
  - Persistence target finalized as SQL for baseline management and initial baseline seeding.
  - Contract strategy chosen as backward-compatible additive evolution.
  - Idempotency and validation strategy defined for rerun-safe seeding.
