# Phase 0 Research — Fabric Foundation Baseline

## Decisions

### 1) Authentication default and examples
- Decision: Document both Service Principal and Managed Identity paths, with Service Principal as setup baseline and Managed Identity as preferred production posture.
- Rationale: Constitution allows both, and customer environments vary in readiness for managed identities at bootstrap time.
- Alternatives considered:
  - Managed Identity only: rejected because some tenants cannot bootstrap first deployment without preconfigured identity plumbing.
  - Service Principal only: rejected because it under-emphasizes production least-secret posture.

### 2) Default capacity SKU
- Decision: Use F2 as shipped default, keep SKU fully parameterized.
- Rationale: Aligns with Principle 7 (start small) and lowers entry cost for first validation.
- Alternatives considered:
  - F4 default: rejected for higher baseline cost and unnecessary v1 footprint.

### 3) Region handling
- Decision: Require explicit region input with a documented sample value; do not hard-code a global default.
- Rationale: Fabric availability and governance constraints are tenant-specific; explicit region avoids silent policy conflicts.
- Alternatives considered:
  - Single hard-coded default region: rejected due to policy and availability mismatch risk.

### 4) Naming convention
- Decision: Standardize on `fiq-{plant_code}-{environment}-{resource}` with lowercase alphanumeric and hyphen, enforcing max length per resource type.
- Rationale: Deterministic names satisfy constitution constraints and simplify parity verification.
- Alternatives considered:
  - Engine-specific naming patterns: rejected because parity and contract diffing become brittle.

### 5) Eventstream ingress for v1
- Decision: Ship a custom-app compatible ingress pattern and sample test payload path, leaving customer source connector mapping out of v1 core.
- Rationale: Keeps v1 minimal while still enabling end-to-end validation of landing and transform behavior.
- Alternatives considered:
  - Preconfigured Event Hubs connector: rejected because it introduces external dependency and tenant setup variance.

### 6) Model runner implementation
- Decision: Standardize on Python model runner using Kusto SDK and idempotent KQL scripts.
- Rationale: Python is cross-platform and easiest for repeatable, scriptable deployment in customer delivery engagements.
- Alternatives considered:
  - CLI-only orchestration (`fab`/`fabric-cicd`): rejected for lower control over ordered idempotent schema operations.

### 7) Bicep/Pulumi Fabric item provisioning approach
- Decision: Accept deploymentScript/command-based Fabric REST orchestration for non-Terraform engines, encapsulated within each engine folder.
- Rationale: Preserves engine independence and parity despite provider surface asymmetry.
- Alternatives considered:
  - Restrict to Terraform only: rejected because it violates engine choice principle.
  - Shared cross-engine helper scripts under one engine: rejected because it breaks deletable single-engine guarantee.

## Best Practices Capture

### IaC parity and engine isolation
- Decision: Mirror four logical modules (`capacity`, `workspace`, `eventhouse`, `eventstream`) across engines and validate output parity via contract checks.
- Rationale: Supports constitution Principle 1 and reduces drift risk.
- Alternatives considered:
  - Different module granularity per engine: rejected due to maintenance and comparison complexity.

### Idempotent schema deployment
- Decision: Restrict core KQL DDL to `.create-merge` and `.create-or-alter`, with ordered table-first then policy application.
- Rationale: Guarantees rerunnable model deployment and recovery after partial failures.
- Alternatives considered:
  - Drop/recreate pattern: rejected due to destructive behavior and downtime risk.

### Extension safety
- Decision: Keep `core/` versioned by project and `extensions/` customer-owned with no core file edits required.
- Rationale: Enforces non-fork customization path and upgrade safety.
- Alternatives considered:
  - Allow direct core edits: rejected because upgrades become conflict-prone.
