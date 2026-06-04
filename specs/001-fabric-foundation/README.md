# Factory IQ — Spec-Kit Bundle

Spec-Driven-Development artifacts for the **Factory IQ Fabric IaC Accelerator**,
authored for **GitHub Copilot + Spec-Kit** to generate the accelerator.

**Owner:** Christophe Gigax — Solution Engineer, Microsoft France
**Created:** 2026-06-02

---

## What this is

This folder is **not** the accelerator — it is the **specification set** that
drives Copilot to build it. Feed these files to Spec-Kit's slash-command flow and
let `/implement` generate the Terraform/Bicep/Pulumi + ISA-95 model code.

The deliverable being specified: a repo where a customer **picks exactly one** of
Terraform, Bicep, or Pulumi to deploy a Microsoft Fabric foundation
(**Eventstream + Eventhouse**) carrying an **ISA-95-conformant** data model they
can customize without forking.

---

## Contents

```
factory-iq-specs/
├── README.md                              ← you are here
├── .specify/
│   └── memory/
│       └── constitution.md                ← highest authority · 7 principles
└── specs/
    └── 001-fabric-foundation/
        ├── spec.md                        ← WHAT & WHY · requirements · [NEEDS CLARIFICATION]
        ├── plan.md                        ← HOW · tech context · constitution check · phases
        ├── data-model.md                  ← ISA-95 schema · tables · update policies
        ├── contracts/
        │   └── connection-contract.md     ← the sacred connection.json contract
        ├── tasks.md                        ← ordered build tasks T001–T036
        └── quickstart.md                   ← customer deploy walkthrough
```

---

## How to use it with Spec-Kit

In a Spec-Kit-enabled repo, the artifacts map to the standard flow:

| Spec-Kit command | Reads / produces | Already provided here |
|---|---|---|
| `/constitution` | `.specify/memory/constitution.md` | ✅ `constitution.md` |
| `/specify` | `specs/<feature>/spec.md` | ✅ `spec.md` |
| `/plan` | `plan.md` + `data-model.md` + `contracts/` | ✅ all three |
| `/tasks` | `tasks.md` | ✅ `tasks.md` |
| `/implement` | generates the code | ▶️ run this |

**Suggested path:**
1. Copy this bundle into your Spec-Kit repo (preserving the `.specify/` and
   `specs/` layout).
2. Resolve the **[NEEDS CLARIFICATION]** markers in `spec.md` (7 questions —
   auth default, default SKU, region, naming, source protocol, runner runtime,
   Fabric-REST approval).
3. Re-run `/plan` if any clarification changes the technical context.
4. Run `/tasks` to confirm the task list, then `/implement` to generate code,
   building **Phase 1 (`shared/`) before any engine**.

---

## The two ideas that make this work

1. **`shared/` is the single source of truth** — the ISA-95 model, the
   Eventstream topology, and `plant-hierarchy.yaml` live exactly once and are
   consumed by all engines, owned by none.
2. **`connection.json` is the seam** — every engine emits the same file; the
   model runner reads only that file and never knows which engine ran. This is
   what lets the three engines stay interchangeable and the customer delete two
   of them.

Both are enforced as **non-negotiable** constitution principles.

---

## Open items before `/implement`

The 7 `[NEEDS CLARIFICATION]` markers in `spec.md`. Everything else is decided.
