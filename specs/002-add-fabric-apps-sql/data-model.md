# Data Model - Fabric Apps ISA-95 Baseline Management

## Overview
This feature introduces a SQL-backed operational baseline model for ISA-95 hierarchy and related operational metadata, while preserving shared ISA-95 definitions under `shared/` as canonical source.

## Entities

### ISA95BaselineNode
- Purpose: Represents baseline hierarchy nodes (Enterprise, Site, Area, WorkCenter, WorkUnit).
- Key fields:
  - `nodeId` (string/UUID, required, immutable)
  - `nodeType` (enum: Enterprise|Site|Area|WorkCenter|WorkUnit, required)
  - `parentNodeId` (string/UUID, nullable only for Enterprise)
  - `displayName` (string, required)
  - `status` (enum: Active|Inactive, default Active)
  - `version` (integer, required for optimistic concurrency)
  - `createdAt`, `createdBy`, `updatedAt`, `updatedBy` (audit metadata)
- Validation rules:
  - `nodeType=Enterprise` must have no parent.
  - Non-Enterprise nodes must reference valid parent type according to ISA-95 hierarchy.
  - Node identifiers must be unique.

### ISA95OperationalRecord
- Purpose: Stores baseline operational reference records associated with hierarchy nodes (production/material/batch/quality references).
- Key fields:
  - `recordId` (string/UUID, required)
  - `nodeId` (FK to ISA95BaselineNode, required)
  - `recordType` (enum: Production|Material|Batch|Quality, required)
  - `payload` (JSON/text, required)
  - `effectiveFrom`, `effectiveTo` (timestamps)
  - `createdAt`, `createdBy`, `updatedAt`, `updatedBy`
- Validation rules:
  - Referenced `nodeId` must exist.
  - `effectiveTo` must be null or greater than `effectiveFrom`.

### BaselineChangeRecord
- Purpose: Immutable audit trail for baseline mutations.
- Key fields:
  - `changeId` (string/UUID or bigint, immutable)
  - `entityType` (BaselineNode|OperationalRecord)
  - `entityId` (required)
  - `action` (Create|Update|Deactivate|Seed)
  - `actor` (user/service identity)
  - `changedAt` (timestamp)
  - `changeSummary` (JSON/text diff summary)
  - `seedRunId` (nullable FK to BaselineSeedRun)
- Validation rules:
  - Immutable after insertion.

### BaselineSeedRun
- Purpose: Tracks each baseline seed execution for idempotency, observability, and diagnostics.
- Key fields:
  - `seedRunId` (UUID, required)
  - `startedAt`, `completedAt` (timestamps)
  - `status` (Running|Succeeded|Failed)
  - `seedSource` (e.g., hierarchy config version/path)
  - `counts` (JSON/object with per-entity write counts)
  - `errorMessage` (nullable)
- Validation rules:
  - `completedAt` required when status is Succeeded/Failed.

## Relationships
- `ISA95BaselineNode` has self-referential parent-child relationship (hierarchy).
- `ISA95OperationalRecord` belongs to exactly one `ISA95BaselineNode`.
- `BaselineChangeRecord` references changed baseline node or operational record.
- `BaselineSeedRun` has zero-to-many related `BaselineChangeRecord` entries tagged with `action=Seed`.

## State Transitions

### ISA95BaselineNode
- Active -> Inactive (soft deactivation)
- Inactive -> Active (reactivation, if parent integrity remains valid)
- Any write operation increments `version`.

### BaselineSeedRun
- Running -> Succeeded
- Running -> Failed
- Terminal states are immutable except for diagnostics enrichment in controlled ops flow.

## Integrity and Idempotency Notes
- Seed writes must use idempotent semantics keyed by stable business identifiers from shared hierarchy config.
- Concurrent updates to `ISA95BaselineNode` must enforce optimistic concurrency using `version`.
- Database constraints must reject orphan hierarchy links.
