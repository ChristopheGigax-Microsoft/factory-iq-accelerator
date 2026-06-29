# Feature Specification: Fabric Apps ISA-95 Baseline Management

**Feature Branch**: `[002-add-fabric-apps-sql]`

**Created**: 2026-06-26

**Status**: Draft

**Input**: User description: "I want to add a specification about a Fabric Apps (Rayfin SDK) to manage the ISA-95 baseline online on the Fabric workspace. This include the app and also a SQL database (for the moment) to manage the data - so you should also change the script that deploy the initial ISA-95 baseline to target the SQL database and not the Eventhouse."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Manage ISA-95 Baseline Online (Priority: P1)

As a manufacturing data owner, I can view and manage the ISA-95 baseline online from a Fabric workspace app so that I can maintain the canonical plant hierarchy and operational entities without editing raw files.

**Why this priority**: This is the core user value. Without online management, baseline governance remains manual and error-prone.

**Independent Test**: Can be fully tested by creating and updating ISA-95 baseline records in the app and verifying the latest approved baseline is persisted and visible in the workspace context.

**Acceptance Scenarios**:

1. **Given** a user opens the Fabric app with baseline access, **When** they view the baseline, **Then** they see the current ISA-95 hierarchy and core attributes in a structured online interface.
2. **Given** a user edits baseline data and submits changes, **When** validation passes, **Then** the updated baseline is saved and immediately available for subsequent reads.

---

### User Story 2 - Persist Baseline in SQL Store (Priority: P1)

As a platform operator, I need baseline data to be persisted in a SQL database so that baseline management has a reliable relational source of record for this phase.

**Why this priority**: Data persistence is required for safe online management and auditability; the app cannot operate meaningfully without it.

**Independent Test**: Can be fully tested by writing baseline records through approved workflows and confirming they can be queried, updated, and version-tracked from the SQL data store.

**Acceptance Scenarios**:

1. **Given** baseline entities are submitted from the app, **When** the save operation completes, **Then** records are persisted in SQL and remain retrievable after session restart.
2. **Given** existing baseline data is present in SQL, **When** a user loads the baseline view, **Then** the app renders data from SQL as the authoritative source.

---

### User Story 3 - Seed Baseline to SQL Instead of Eventhouse (Priority: P2)

As a deployment engineer, I can run the initial baseline deployment script to seed the SQL database (not Eventhouse) so that new environments start with the expected ISA-95 baseline in the correct target store.

**Why this priority**: The app rollout depends on consistent initial data in the same system used for runtime management.

**Independent Test**: Can be fully tested by running the baseline seed process in a fresh environment and verifying baseline data exists in SQL while no new baseline writes are directed to Eventhouse.

**Acceptance Scenarios**:

1. **Given** a new environment is provisioned, **When** the initial baseline deployment script runs, **Then** baseline entities are seeded into SQL successfully.
2. **Given** seeding is complete, **When** validation checks are executed, **Then** baseline tables contain expected ISA-95 starter data and Eventhouse is not used as the baseline write target.

### Edge Cases

- What happens when the baseline deployment script is executed multiple times in the same environment? The process must be idempotent and must not create duplicate logical baseline entities.
- How does the system handle invalid hierarchy submissions (for example, a Work Unit linked to a missing Work Center)? The system must reject the change with actionable error feedback and preserve existing valid data.
- What happens when concurrent users attempt to update the same baseline record? The system must prevent silent overwrites and require an explicit conflict resolution path.
- How does the app behave if SQL is temporarily unavailable? Users must receive a clear failure message and no partial baseline changes should be committed.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a Fabric app experience, based on Rayfin SDK, for online management of ISA-95 baseline data.
- **FR-002**: System MUST allow authorized users to create, read, update, and review ISA-95 baseline entities through the app.
- **FR-003**: System MUST persist ISA-95 baseline data in a SQL database as the authoritative store for this phase.
- **FR-004**: System MUST enforce baseline data integrity rules for ISA-95 hierarchy relationships before saving changes.
- **FR-005**: System MUST track baseline change metadata, including who changed data and when, for operational traceability.
- **FR-006**: System MUST provide a repeatable initial baseline deployment flow that seeds SQL with starter ISA-95 baseline data.
- **FR-007**: System MUST update the existing baseline seeding behavior so that initial baseline data targets SQL instead of Eventhouse.
- **FR-008**: System MUST ensure repeated baseline seed executions do not create duplicate baseline records.
- **FR-009**: System MUST preserve compatibility with existing shared ISA-95 model definitions as the source for baseline structure.
- **FR-010**: System MUST provide clear user-facing error feedback when baseline operations fail validation or persistence.

### Key Entities *(include if feature involves data)*

- **ISA95BaselineNode**: A baseline hierarchy entity (Enterprise, Site, Area, Work Center, Work Unit) with identity, parent relationship, classification, and status attributes.
- **ISA95OperationalRecord**: Baseline operational entities associated with equipment hierarchy, such as production, material, batch, or quality-related baseline definitions.
- **BaselineChangeRecord**: Immutable audit entry describing baseline change action, actor, timestamp, affected entity, and change summary.
- **BaselineSeedRun**: Execution record of an initial baseline deployment run, including run status, execution time, and seeded entity counts.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 95% of authorized users can complete a baseline create or update task in under 3 minutes during acceptance testing.
- **SC-002**: 100% of successful baseline writes in test and pre-production environments are persisted in SQL and retrievable through the app.
- **SC-003**: Initial baseline seeding succeeds in at least 99% of fresh environment runs without manual intervention.
- **SC-004**: Re-running the baseline seed process in the same environment produces zero unintended duplicate logical baseline entities.
- **SC-005**: At least 90% of critical baseline validation failures are presented with actionable, user-understandable error messages in usability review.

## Assumptions

- Fabric workspace access controls and user identity are already available and can be reused for authorization.
- SQL database is the temporary authoritative baseline store for this phase, with potential future evolution to a different persistence backend.
- Existing ISA-95 shared model assets remain the canonical model definition and are reused rather than redefined.
- Scope includes specification for app behavior, baseline persistence, and initial seeding target change, but excludes broader analytics or reporting redesign.
- Existing deployment workflows remain in place, with only baseline seeding target behavior changing from Eventhouse to SQL.
