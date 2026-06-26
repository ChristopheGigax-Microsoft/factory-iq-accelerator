# Tasks: Fabric Apps ISA-95 Baseline Management

**Input**: Design documents from `/specs/002-add-fabric-apps-sql/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Test tasks are not included because the feature specification does not explicitly request TDD or dedicated automated test deliverables in this phase.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize folders and baseline scaffolding for Fabric app plus SQL baseline flow.

- [x] T001 Bootstrap Fabric app using the official Rayfin command `npm create @microsoft/rayfin@latest` in src/fabric-apps/ and create baseline-app/ (do not hand-roll app scaffolding)
- [x] T002 Create SQL baseline artifact folder and readme in shared/sql/baseline/README.md
- [x] T003 [P] Add SQL runner dependency manifest in shared/scripts/requirements-sql.txt
- [x] T004 [P] Add Fabric app workspace integration readme in src/fabric-apps/README.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core contract and shared plumbing required before any user story work.

**CRITICAL**: No user story work can begin until this phase is complete.

- [x] T005 Extend output contract with additive SQL metadata rules in contracts/connection-contract.md
- [x] T006 Update runner stage contract for SQL target behavior in contracts/model-runner-interface.md
- [x] T007 [P] Implement SQL connection parsing and validation utility in shared/scripts/sql_connection.py
- [x] T008 [P] Implement baseline repository abstraction and shared exceptions in shared/scripts/baseline_repository.py
- [x] T009 Implement hierarchy YAML to baseline entity mapping utility in shared/scripts/hierarchy_mapper.py
- [x] T010 Update deployment and configuration documentation for SQL baseline target in README.md and shared/isa95-model/README.md

**Checkpoint**: Foundation ready. User stories can now proceed.

---

## Phase 3: User Story 1 - Manage ISA-95 Baseline Online (Priority: P1) MVP

**Goal**: Deliver a Fabric app experience for viewing and editing ISA-95 baseline data online.

**Independent Test**: Launch the app, read baseline hierarchy, perform create/update action, and verify updated state is immediately available through the app workflow.

### Implementation for User Story 1

- [x] T011 [P] [US1] Scaffold Rayfin app entrypoint for baseline management in src/fabric-apps/src/main.tsx and src/fabric-apps/package.json
- [x] T012 [P] [US1] Implement baseline hierarchy read integration in src/fabric-apps/src/services/baselineClient.ts
- [x] T013 [P] [US1] Implement baseline create/update flow with optimistic concurrency input in src/fabric-apps/src/services/baselineClient.ts and src/fabric-apps/src/pages/BaselineManager.tsx
- [x] T014 [US1] Extend Rayfin SDK bootstrapped app with baseline manager page in src/fabric-apps/src/App.tsx and src/fabric-apps/src/pages/BaselineManager.tsx
- [x] T015 [US1] Implement app-side baseline client integration in src/fabric-apps/src/services/baselineClient.ts
- [x] T016 [US1] Implement auth context and actionable error rendering in src/fabric-apps/src/services/authContext.ts and src/fabric-apps/src/components/ErrorBanner.tsx
- [x] T017 [US1] Add user-story smoke steps for online baseline management in specs/002-add-fabric-apps-sql/quickstart.md

**Checkpoint**: User Story 1 is independently functional in workspace app flow.

---

## Phase 4: User Story 2 - Persist Baseline in SQL Store (Priority: P1)

**Goal**: Implement SQL as authoritative persistence for baseline nodes, operational records, and audit metadata.

**Independent Test**: Submit baseline writes and verify records, versions, and audit fields are persisted and retrievable from SQL after restart.

### Implementation for User Story 2

- [x] T018 [P] [US2] Create SQL core tables for baseline hierarchy and operational records in shared/sql/baseline/010_core_tables.sql
- [x] T019 [P] [US2] Create SQL audit and seed-run tables in shared/sql/baseline/020_audit_tables.sql
- [x] T020 [US2] Add hierarchy integrity constraints and version concurrency rules in shared/sql/baseline/030_constraints.sql
- [x] T021 [US2] Implement SQL repository adapter for baseline CRUD operations in shared/scripts/sql_baseline_repository.py
- [x] T022 [US2] Integrate baseline persistence flow with SQL repository and audit attribution in shared/scripts/sql_baseline_repository.py
- [x] T023 [US2] Implement SQL error-to-domain error mapping for user-facing responses in shared/scripts/sql_baseline_repository.py
- [x] T024 [US2] Update feature contracts with SQL persistence semantics in specs/002-add-fabric-apps-sql/contracts/fabric-baseline-app-interface.md and specs/002-add-fabric-apps-sql/contracts/sql-seed-runner-contract.md
- [x] T025 [US2] Add SQL verification queries and persistence checks in specs/002-add-fabric-apps-sql/quickstart.md

**Checkpoint**: User Story 2 is independently functional with SQL persistence and auditability.

---

## Phase 5: User Story 3 - Seed Baseline to SQL Instead of Eventhouse (Priority: P2)

**Goal**: Change initial baseline deployment script target to SQL with idempotent, rerunnable behavior.

**Independent Test**: Execute seed flow twice in a fresh environment and verify SQL contains expected baseline data with no duplicate logical entities and no Eventhouse baseline writes.

### Implementation for User Story 3

- [x] T026 [P] [US3] Implement SQL idempotent seed command generation helpers in shared/scripts/sql_seed_commands.py
- [x] T027 [US3] Update deploy runner to execute SQL baseline seed target logic in shared/scripts/deploy-model.py
- [x] T028 [US3] Refactor shared hierarchy validation usage between seed and deploy flows in shared/scripts/seed-hierarchy.py and shared/scripts/deploy-model.py
- [x] T029 [US3] Add baseline seed run lifecycle tracking and failure status updates in shared/scripts/deploy-model.py
- [x] T030 [US3] Update operator runbook steps for SQL-target seeding in specs/002-add-fabric-apps-sql/quickstart.md and README.md

**Checkpoint**: User Story 3 is independently functional with rerunnable SQL seeding.

---

## Phase 6: Polish and Cross-Cutting Concerns

**Purpose**: Final consistency, operational readiness, and documentation hardening across stories.

- [x] T031 [P] Document architecture updates for Fabric app and SQL baseline in docs/architecture.md and CONTRACT.md
- [x] T032 [P] Add SQL baseline troubleshooting guidance in contracts/model-runner-interface.md and shared/sql/baseline/README.md
- [x] T033 [P] Add Terraform and Bicep rollout notes for SQL baseline support in infra/bicep/README.md and infra/terraform/README.md
- [x] T034 Validate full quickstart and checklist alignment for all stories in specs/002-add-fabric-apps-sql/quickstart.md and specs/002-add-fabric-apps-sql/checklists/requirements.md

---

## Dependencies and Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies.
- **Phase 2 (Foundational)**: Depends on Phase 1 and blocks all user stories.
- **Phase 3 (US1)**: Depends on Phase 2.
- **Phase 4 (US2)**: Depends on Phase 2.
- **Phase 5 (US3)**: Depends on Phase 2 and Phase 4 outputs for SQL persistence components.
- **Phase 6 (Polish)**: Depends on completion of selected user stories.

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational phase, but independent acceptance requires SQL persistence capabilities from US2 (minimum T018-T023).
- **US2 (P1)**: Can start after Foundational phase; provides SQL persistence backbone used by US1 and US3.
- **US3 (P2)**: Depends on US2 SQL persistence primitives and contract updates.

### Suggested Completion Order

1. Setup to Foundational
2. US2 and US1 in parallel (merge integration after each checkpoint)
3. US3
4. Polish

---

## Parallel Execution Examples

### User Story 1

- Run T011, T012, and T013 in parallel after Phase 2 completion.
- Run T014 and T015 in parallel once API route contracts are defined.

### User Story 2

- Run T018 and T019 in parallel.
- Run T021 and T023 in parallel after T020.

### User Story 3

- Run T026 in parallel with documentation prep work in T030.
- Run T028 after T027 starts, then complete T029 once SQL seed path is operational.

---

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Setup and Foundational phases.
2. Complete minimum US2 persistence prerequisites (T018-T023).
3. Complete US1 app flow (T011-T017).
4. Validate independent US1 acceptance criteria.

### Incremental Delivery

1. Deliver US2 SQL persistence capability (minimum required for US1 acceptance).
2. Deliver US1 online management experience.
3. Deliver US3 SQL-target seeding migration.
4. Finish with cross-cutting documentation and operational polish.

### Parallel Team Strategy

1. Team A: US2 SQL schema and repository (T018-T023).
2. Team B: US1 UI and API integration (T011-T017).
3. Team C: US3 seed migration (T026-T030) once Team A core SQL components are available.

---

## Phase 7: Measurement and Validation Gates

**Purpose**: Add explicit verification work for measurable success criteria and reproducible bootstrap steps.

- [x] T035 Pin and validate Rayfin bootstrap command `npm create @microsoft/rayfin@latest` and follow-up `npx rayfin up` in specs/002-add-fabric-apps-sql/quickstart.md
- [x] T036 Add SC-001 timing measurement procedure and evidence capture template in specs/002-add-fabric-apps-sql/quickstart.md
- [x] T037 Add SC-003 seed success-rate measurement procedure and run log format in specs/002-add-fabric-apps-sql/quickstart.md
- [x] T038 Add SC-005 validation error usability review checklist and evidence log in specs/002-add-fabric-apps-sql/checklists/requirements.md
