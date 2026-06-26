# Contract: Fabric Baseline App Interface

## Purpose
Defines the external behavior contract for the Fabric workspace app (Rayfin SDK) that manages ISA-95 baseline data online.

## Actors
- Baseline Viewer: read-only baseline access.
- Baseline Editor: create/update/deactivate baseline entities.
- Baseline Admin: seed oversight and operational administration.

## Functional Surface

### Read Baseline Hierarchy
- Operation: `getBaselineHierarchy`
- API Path: `GET /baseline/hierarchy`
- Input:
  - `workspaceId` (required)
  - `includeInactive` (optional, default false)
- Output:
  - Hierarchical tree of ISA95BaselineNode entities.
- Errors:
  - `AUTH_FORBIDDEN`
  - `BASELINE_NOT_FOUND`
  - `STORE_UNAVAILABLE`

### Create Baseline Node
- Operation: `createBaselineNode`
- API Path: `POST /baseline/nodes`
- Input:
  - `nodeType`, `parentNodeId`, `displayName`, optional metadata
- Output:
  - Created node with `nodeId`, `version`, audit metadata
- Validation:
  - Enforce ISA-95 parent/child compatibility rules.

### Update Baseline Node
- Operation: `updateBaselineNode`
- API Path: `POST /baseline/nodes` (upsert with optimistic concurrency)
- Input:
  - `nodeId` (required)
  - `version` (required, optimistic concurrency token)
  - patch payload
- Output:
  - Updated node with incremented `version`
- Errors:
  - `CONFLICT_VERSION_MISMATCH`
  - `VALIDATION_FAILED`

### Seed Status Query
- Operation: `getSeedRunStatus`
- Input:
  - `seedRunId` or latest flag
- Output:
  - BaselineSeedRun status and summary counts

## Non-Functional Constraints
- Writes must be audit-attributed to user identity and timestamp.
- Validation failures must return actionable messages.
- App contract must remain decoupled from specific IaC engine implementation.
- API errors must map to user actionable banners (`CONFLICT_VERSION_MISMATCH`, `VALIDATION_FAILED`, `STORE_UNAVAILABLE`).
