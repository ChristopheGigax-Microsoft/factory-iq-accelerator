# Implementation Plan: Fabric Foundation Baseline

**Branch**: `[001-speckit-specify]` | **Date**: 2026-06-02 | **Spec**: `specs/001-fabric-foundation/spec.md`

**Input**: Feature specification from `/specs/001-fabric-foundation/spec.md`

## Summary

Provision a minimum Microsoft Fabric foundation for one plant with identical business outcomes across Terraform, Bicep, and Pulumi, while keeping ISA-95 model assets engine-agnostic in `shared/` and enforcing a stable handoff contract through `connection.json`.

## Technical Context

**Language/Version**: HCL (Terraform), Bicep, TypeScript (Pulumi), Python 3.11+ (model runner), KQL DDL

**Primary Dependencies**: Terraform `microsoft/fabric` provider, Azure ARM/Bicep deployment scripts, Pulumi `azure-native`, Azure Kusto Python SDK, YAML parser

**Storage**: Microsoft Fabric Eventhouse KQL database plus YAML/JSON configuration files

**Testing**: Terraform/Bicep/Pulumi validation commands, Python runner smoke tests, idempotency rerun tests, contract conformance checks

**Target Platform**: Azure + Microsoft Fabric control plane, local developer shell (Windows bash compatible)

**Project Type**: Multi-engine infrastructure accelerator with shared model assets and helper scripts

**Performance Goals**: Baseline deployment completes reliably for one plant; idempotent reruns produce no unintended resource/schema drift

**Constraints**: Single-engine deployment per run, no secrets in repo, deterministic naming from `plant_code` + `environment`, output contract parity across engines

**Scale/Scope**: v1 is one workspace per plant and core scope only (capacity/workspace/eventhouse/eventstream + ISA-95 model seeding)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate Question | Initial Status |
|---|---|---|
| 1. Engine Independence | Are engines isolated and deletable to one engine with mirrored modules? | PASS |
| 2. Shared Model SSOT | Are ISA-95 schema, topology, and plant config defined once under `shared/`? | PASS |
| 3. Output Contract | Will each engine emit the same `connection.json` and keep model runner engine-blind? | PASS |
| 4. ISA-95 Conformance | Do entities and hierarchy follow ISA-95 core semantics with extensions isolated? | PASS |
| 5. Idempotency | Are IaC/model operations rerunnable without duplicate side effects? | PASS |
| 6. Customizability | Can customer extensions and plant hierarchy changes happen without core edits? | PASS |
| 7. Start Small | Is v1 minimal footprint and scale-up parameterized? | PASS |

No constitutional violations detected before research.

## Project Structure

### Documentation (this feature)

```text
specs/001-fabric-foundation/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── connection-contract.md
│   └── model-runner-interface.md
└── tasks.md
```

### Source Code (repository root)

```text
shared/
├── isa95-model/
│   ├── core/
│   ├── extensions/
│   └── config/
├── eventstream/
└── scripts/

infra/
├── terraform/
│   └── modules/{capacity,workspace,eventhouse,eventstream}/
├── bicep/
│   ├── modules/
│   └── scripts/
└── pulumi/
    └── src/

contracts/
└── connection-contract.md
```

**Structure Decision**: Keep all technology-agnostic model/topology/config in `shared/`, mirror the four-module structure in each `infra/` engine folder, and enforce interop via `connection.json` contract.

## Phase 0 Research Plan

1. Resolve default authentication guidance for customer delivery docs.
2. Confirm default sizing/region/naming conventions compatible with constitution constraints.
3. Lock model runner approach and idempotent KQL patterns.
4. Record engine parity strategy where non-Terraform engines rely on Fabric REST orchestration.

## Phase 1 Design Plan

1. Extract canonical data entities and lifecycle semantics into `data-model.md`.
2. Define explicit contracts under `contracts/` for deployment handoff and model runner inputs.
3. Draft executable `quickstart.md` for single-engine deployment and model-seed verification.
4. Re-run constitution gate after design artifacts are authored.

## Post-Design Constitution Re-Check

| Principle | Post-Design Status | Notes |
|---|---|---|
| 1. Engine Independence | PASS | Contracts and quickstart preserve one-engine-at-a-time flow |
| 2. Shared Model SSOT | PASS | Data model centers on `shared/` ownership boundaries |
| 3. Output Contract | PASS | Connection contract documented as single handoff artifact |
| 4. ISA-95 Conformance | PASS | Data model enforces ISA-95 hierarchy/facts and extension boundaries |
| 5. Idempotency | PASS | Research and contracts require rerunnable IaC and KQL operations |
| 6. Customizability | PASS | Quickstart includes extension and hierarchy-only customization path |
| 7. Start Small | PASS | Scope remains v1-minimal with parameterized scaling |

## Complexity Tracking

No justified complexity exceptions were required.

## Release Validation Notes

- Setup, foundational, and all user-story tasks have been implemented.
- Engine artifacts exist for Terraform, Bicep, and Pulumi with mirrored logical modules.
- Shared model runner and hierarchy validator are present and wired through documented contracts.
- Quickstart includes validation and idempotency rerun sequences.
