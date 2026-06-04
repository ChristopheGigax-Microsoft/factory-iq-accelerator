# Tasks: Fabric Foundation Baseline

**Input**: Design documents from `/specs/001-fabric-foundation/`
**Prerequisites**: `plan.md` (required), `spec.md` (required), `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: No explicit TDD/test-first requirement was requested in the specification; validation tasks are included as implementation tasks.

**Organization**: Tasks are grouped by user story to enable independent implementation and validation.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create baseline repository scaffolding and global docs that all stories rely on.

- [x] T001 Create root accelerator README with engine-selection guidance in README.md
- [x] T002 Create output contract summary doc in CONTRACT.md
- [x] T003 [P] Create shared model directories with placeholders in shared/isa95-model/core/.gitkeep
- [x] T004 [P] Create shared extension placeholder in shared/isa95-model/extensions/.gitkeep
- [x] T005 [P] Create shared configuration and eventstream definition placeholders in shared/isa95-model/config/plant-hierarchy.yaml
- [x] T006 [P] Add repository ignore rules for state/secrets/connection artifacts in .gitignore

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish architecture-wide contracts, deterministic naming, and shared runner scaffolding required before any user story work.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T007 Implement canonical naming convention specification in docs/architecture.md
- [x] T008 [P] Implement connection contract schema reference in contracts/connection-contract.md
- [x] T009 [P] Implement model runner invocation contract in contracts/model-runner-interface.md
- [x] T010 Create shared eventstream baseline topology definition in shared/eventstream/definition/eventstream.json
- [x] T011 Create model deployment runner entrypoint and stage orchestration in shared/scripts/deploy-model.py
- [x] T012 [P] Create plant hierarchy seed runner in shared/scripts/seed-hierarchy.py
- [x] T013 Add shared quick validation command sequence in specs/001-fabric-foundation/quickstart.md

**Checkpoint**: Foundation complete; user story implementation can begin.

---

## Phase 3: User Story 1 - Deploy a starter plant foundation (Priority: P1) 🎯 MVP

**Goal**: Deliver a complete single-engine (Terraform) path that deploys baseline Fabric resources and emits contract-compliant `connection.json`.

**Independent Test**: Provide `plantCode`, `environment`, `region`, run Terraform deployment once, verify baseline resources plus valid `connection.json`, then run model runner successfully.

### Implementation for User Story 1

- [x] T014 [P] [US1] Create Terraform capacity module resources in infra/terraform/modules/capacity/main.tf
- [x] T015 [P] [US1] Create Terraform workspace module resources in infra/terraform/modules/workspace/main.tf
- [x] T016 [P] [US1] Create Terraform eventhouse module resources in infra/terraform/modules/eventhouse/main.tf
- [x] T017 [P] [US1] Create Terraform eventstream module resources in infra/terraform/modules/eventstream/main.tf
- [x] T018 [US1] Compose Terraform module wiring and variables in infra/terraform/main.tf
- [x] T019 [US1] Define Terraform input variables and defaults in infra/terraform/variables.tf
- [x] T020 [US1] Emit deployment outputs including contract fields in infra/terraform/outputs.tf
- [x] T021 [US1] Add Terraform dev/prod parameter examples in infra/terraform/environments/dev.tfvars
- [x] T022 [US1] Implement Terraform-generated connection artifact documentation and command in infra/terraform/README.md
- [x] T023 [US1] Implement ISA-95 core dimensions in shared/isa95-model/core/00_dimensions.kql
- [x] T024 [US1] Implement ISA-95 operational facts in shared/isa95-model/core/10_facts.kql
- [x] T025 [US1] Implement telemetry landing-to-fact update policies in shared/isa95-model/core/20_update_policies.kql

**Checkpoint**: User Story 1 is independently deployable and validates MVP value.

---

## Phase 4: User Story 2 - Reuse the same process across infrastructure engines (Priority: P2)

**Goal**: Achieve parity for Bicep and Pulumi so any one chosen engine produces equivalent outcomes and handoff contract.

**Independent Test**: Deploy with Bicep and Pulumi using equivalent inputs, validate generated `connection.json` fields match contract and semantic parity with Terraform outcomes.

### Implementation for User Story 2

- [x] T026 [P] [US2] Implement Bicep capacity module in infra/bicep/modules/capacity.bicep
- [x] T027 [P] [US2] Implement Bicep workspace/item orchestration script in infra/bicep/scripts/create-fabric-items.ps1
- [x] T028 [US2] Wire Bicep modules and deployment script in infra/bicep/main.bicep
- [x] T029 [US2] Add Bicep environment parameter samples in infra/bicep/environments/dev.bicepparam
- [x] T030 [US2] Document Bicep deployment and contract output flow in infra/bicep/README.md
- [x] T031 [P] [US2] Implement Pulumi capacity component in infra/pulumi/src/capacity.ts
- [x] T032 [P] [US2] Implement Pulumi workspace component in infra/pulumi/src/workspace.ts
- [x] T033 [P] [US2] Implement Pulumi eventhouse component in infra/pulumi/src/eventhouse.ts
- [x] T034 [P] [US2] Implement Pulumi eventstream component in infra/pulumi/src/eventstream.ts
- [x] T035 [US2] Wire Pulumi program and stack config mapping in infra/pulumi/index.ts
- [x] T036 [US2] Add Pulumi project and stack configuration defaults in infra/pulumi/Pulumi.yaml
- [x] T037 [US2] Document Pulumi deployment and contract output flow in infra/pulumi/README.md

**Checkpoint**: User Stories 1 and 2 both operate with engine-independent outcomes.

---

## Phase 5: User Story 3 - Customize the plant model without changing core assets (Priority: P3)

**Goal**: Enable customer extensions and hierarchy edits through `extensions/` and YAML-only changes without touching core model files.

**Independent Test**: Add an extension script and modify hierarchy YAML, rerun model deployment, and verify changes apply while core files remain unchanged.

### Implementation for User Story 3

- [x] T038 [US3] Create extension deployment ordering convention in shared/isa95-model/extensions/README.md
- [x] T039 [US3] Provide sample customer extension script in shared/isa95-model/extensions/30_sample_tool_entity.kql
- [x] T040 [US3] Implement editable plant hierarchy seed template in shared/isa95-model/config/plant-hierarchy.yaml
- [x] T041 [US3] Update model runner to process extensions after core scripts in shared/scripts/deploy-model.py
- [x] T042 [US3] Update hierarchy seeding logic with parent-child validation in shared/scripts/seed-hierarchy.py
- [x] T043 [US3] Document extension and hierarchy-only customization workflow in shared/isa95-model/README.md

**Checkpoint**: All user stories are independently functional and customer customization is non-fork.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Complete parity checks, idempotency checks, and release-ready guidance across stories.

- [x] T044 [P] Add cross-engine parity validation checklist in docs/architecture.md
- [x] T045 [P] Add idempotency rerun verification steps in specs/001-fabric-foundation/quickstart.md
- [x] T046 Add contract conformance validation command examples in contracts/connection-contract.md
- [x] T047 Add model runner failure triage and rerun guidance in contracts/model-runner-interface.md
- [x] T048 Run end-to-end quickstart validation and capture release notes in specs/001-fabric-foundation/plan.md

---

## Dependencies & Execution Order

### Phase Dependencies

- Phase 1 (Setup): no dependencies.
- Phase 2 (Foundational): depends on Phase 1 and blocks all user stories.
- Phase 3 (US1): depends on Phase 2.
- Phase 4 (US2): depends on Phase 2 and can proceed after US1 starts, but parity comparison depends on US1 completion.
- Phase 5 (US3): depends on Phase 2 and model runner/core schema from US1.
- Phase 6 (Polish): depends on completion of selected user stories.

### User Story Dependencies

- US1 (P1): first deliverable and MVP, no dependency on other stories.
- US2 (P2): functionally independent after Phase 2; semantic parity checks compare against US1 outcomes.
- US3 (P3): independent customization flow after shared runner/core schema foundation is in place.

### Parallel Opportunities

- Setup: T003, T004, T005, T006 can run in parallel.
- Foundational: T008, T009, T012 can run in parallel.
- US1: T014, T015, T016, T017 can run in parallel before T018.
- US2: T026 and T027 can run in parallel; T031, T032, T033, T034 can run in parallel.
- US3: T041 and T042 can run in parallel after T040 baseline exists.
- Polish: T044 and T045 can run in parallel.

---

## Parallel Example: User Story 1

```bash
# Parallel module authoring for Terraform MVP:
Task: "T014 [US1] Create Terraform capacity module resources in infra/terraform/modules/capacity/main.tf"
Task: "T015 [US1] Create Terraform workspace module resources in infra/terraform/modules/workspace/main.tf"
Task: "T016 [US1] Create Terraform eventhouse module resources in infra/terraform/modules/eventhouse/main.tf"
Task: "T017 [US1] Create Terraform eventstream module resources in infra/terraform/modules/eventstream/main.tf"
```

## Parallel Example: User Story 2

```bash
# Parallel Pulumi component authoring:
Task: "T031 [US2] Implement Pulumi capacity component in infra/pulumi/src/capacity.ts"
Task: "T032 [US2] Implement Pulumi workspace component in infra/pulumi/src/workspace.ts"
Task: "T033 [US2] Implement Pulumi eventhouse component in infra/pulumi/src/eventhouse.ts"
Task: "T034 [US2] Implement Pulumi eventstream component in infra/pulumi/src/eventstream.ts"
```

## Parallel Example: User Story 3

```bash
# Parallel runner updates after hierarchy template baseline:
Task: "T041 [US3] Update model runner to process extensions after core scripts in shared/scripts/deploy-model.py"
Task: "T042 [US3] Update hierarchy seeding logic with parent-child validation in shared/scripts/seed-hierarchy.py"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phases 1 and 2.
2. Deliver Phase 3 (US1) as the first usable increment.
3. Validate independent deployment + model seed flow before expanding engine coverage.

### Incremental Delivery

1. US1 establishes baseline deployment and model seed path.
2. US2 adds Bicep/Pulumi parity without changing shared semantics.
3. US3 adds customer-safe extension and hierarchy customization.
4. Phase 6 finalizes parity/idempotency and release validation.

### Parallel Team Strategy

1. Team aligns on Phases 1 and 2 first.
2. Split by story after foundation:
   - Engineer A: US1 Terraform + core schema.
   - Engineer B: US2 Bicep/Pulumi parity.
   - Engineer C: US3 customization and seeding flows.
3. Rejoin for Phase 6 cross-cutting validation.
