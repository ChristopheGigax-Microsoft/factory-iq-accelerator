# Factory IQ Accelerator — Constitution

<!--
Governance document for the Factory IQ Fabric IaC Accelerator.
This is the highest-authority artifact. Every spec, plan, and task MUST
comply with the principles below. When Copilot/Spec-Kit generates code,
these principles are non-negotiable acceptance gates.
-->

**Version:** 1.1.0
**Ratified:** 2026-06-02
**Last amended:** 2026-06-26
**Owner:** Christophe Gigax — Solution Engineer, Microsoft France

---

## Purpose

The Factory IQ Accelerator is a reusable, customer-deliverable Infrastructure-as-Code
asset that provisions the Microsoft Fabric foundation for agentic manufacturing
operations — starting with **Eventstream** and **Eventhouse**, modeled to the
**ISA-95** standard. It is designed to *land small and expand*, and to be handed to
a customer who can customize it — especially the data model — without forking the core.

---

## Core Principles

### Principle 1 — Engine Independence (NON-NEGOTIABLE)
The customer chooses **exactly one** Infrastructure-as-Code technology:
**Terraform** or **Bicep**. The two engines are interchangeable and
MUST NOT be mixed in a single deployment.
- Each engine lives in its own self-contained folder under `infra/`.
- A customer MUST be able to delete the engine they did not choose and still
  have a complete, working stack.
- The two engines MUST reach **feature parity**: same resources, same inputs,
  same outputs.
- Engines MUST expose the same four logical modules with mirrored names:
  `capacity`, `workspace`, `eventhouse`, `eventstream`.

### Principle 2 — Shared Model is the Single Source of Truth (NON-NEGOTIABLE)
The ISA-95 data model, the Eventstream topology, and the plant configuration are
**technology-agnostic** and live exactly once under `shared/`.
- `shared/` is consumed by all engines and **owned by none**.
- The ISA-95 schema, Eventstream definition, and plant config MUST NOT be duplicated,
  forked, or embedded inside any engine folder.
- Changing the data model MUST require editing only `shared/`, never an engine.

### Principle 3 — The Output Contract is Sacred (NON-NEGOTIABLE)
Every engine, after a successful deployment, MUST emit an identical
`connection.json` conforming to `contracts/connection-contract.md`.
- The model-deployment step is **engine-blind**: it reads only `connection.json`.
- No engine may require the model runner to know which technology produced it.
- Changing the contract is a breaking change and requires a constitution amendment.

### Principle 4 — ISA-95 Conformance
The **core** data model MUST follow the ISA-95 equipment hierarchy
(Enterprise → Site → Area → Work Center → Work Unit) and ISA-95 operations concepts
(equipment state, production, material, batch, quality).
- Core schema MUST NOT deviate from ISA-95 role-based equipment naming.
- Customer-specific entities belong in `extensions/`, never in `core/`.

### Principle 5 — Idempotency by Construction
Every schema and topology operation MUST be safely re-runnable.
- KQL DDL MUST use idempotent commands (`.create-merge`, `.create-or-alter`).
- IaC MUST be declarative and convergent; re-applying a deployed environment
  produces no unintended changes.
- The model runner MUST tolerate partial prior runs.

### Principle 6 — Customizability Without Forking
The customer MUST be able to extend the solution without editing the core.
- **core/** (you own & version) vs **extensions/** (customer-owned) are strictly separated.
- An upgrade to `core/` MUST NEVER clobber customer changes in `extensions/`.
- Plant-specific reality (sites, lines, equipment, tag mappings) is **config-driven**
  via `plant-hierarchy.yaml` — editable without touching code.
- `core/` is consumed as a pinned, versioned artifact.

### Principle 7 — Start Small, Expand by Parameter
The accelerator lands with the minimum viable footprint and grows by parameter.
- Default capacity SKU is the smallest viable (F2); scaling up is a variable change.
- One workspace per plant; a new plant is a new parameter set, not new code.
- v1 scope is intentionally focused and includes: Eventstream + Eventhouse + ISA-95 model, with an optional Fabric app + SQL baseline management path when explicitly enabled by feature scope.

---

## Constraints & Standards

- **Languages:** HCL (Terraform), Bicep, Python (model runner), KQL (model).
- **Authentication:** Service Principal or Managed Identity; no secrets in source control.
- **Naming:** every resource name derives from a `plant_code` + `environment` parameter.
- **State/secrets:** remote state and secret stores configured per engine; never committed.
- **Documentation:** every engine folder ships a self-contained `README.md`.

---

## Governance

- This constitution supersedes all other practices. Specs and plans that conflict
  with it are invalid until reconciled.
- **Amendments** require: a documented rationale, a version bump, and a migration note
  for any affected specs.
- **Versioning** follows semantic versioning:
  - MAJOR — removal/redefinition of a principle or the output contract.
  - MINOR — a new principle or materially expanded guidance.
  - PATCH — clarifications and wording.
- **Compliance:** every `plan.md` MUST include a "Constitution Check" gate. Any
  violation MUST be justified in a "Complexity Tracking" section or the plan is rejected.

## Migration Note (v1.1.0)

- Principle 7 scope wording is expanded to explicitly allow Fabric app and SQL baseline management within v1 when selected by feature scope.
- Existing specs/plans that previously tracked this as a Principle 7 exception should be updated to remove that exception where applicable.

**Version:** 1.1.0 | **Ratified:** 2026-06-02 | **Last amended:** 2026-06-26
