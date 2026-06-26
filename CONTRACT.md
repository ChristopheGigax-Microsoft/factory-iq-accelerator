# Output Contract Summary

The accelerator defines a single deployment handoff artifact: `connection.json`.

## Purpose

`connection.json` decouples infrastructure provisioning from model deployment.
Any supported engine can produce it; model deployment consumes it without engine-specific logic.

## Required Fields

- `tenantId`
- `subscriptionId`
- `resourceGroup`
- `region`
- `workspaceId`
- `eventhouseId`
- `kqlDatabase`
- `generatedAt`
- `schemaVersion`

## Contract Authority

Canonical contract definition:

- `contracts/connection-contract.md`

Model runner interface:

- `contracts/model-runner-interface.md`

## Compatibility

- Breaking field/semantic changes require governance review.
- Additional diagnostic fields may be included and ignored by the runner.
- SQL baseline metadata is additive and optional under `sqlBaseline`; required v1.0 fields remain unchanged.
