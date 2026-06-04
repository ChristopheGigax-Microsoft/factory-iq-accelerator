# Contract: Model Runner Interface

Purpose: Define how schema deployment tooling is invoked and what behavior is guaranteed.

## Inputs

- `--connection <path>`: path to `connection.json`.
- `--core-dir <path>`: path to shared core KQL scripts.
- `--extensions-dir <path>`: path to customer extension scripts.
- `--hierarchy-config <path>`: path to `plant-hierarchy.yaml`.
- `--fail-on-warning` (optional): treats warnings as failures.

## Behavior contract

- Runner validates `connection.json` before applying scripts.
- Runner applies core schema in deterministic order.
- Runner applies extensions after core.
- Runner applies hierarchy seed after schema availability.
- Runner is idempotent and safe to execute repeatedly.

## Output contract

- Exit code `0` on success/no-op.
- Exit code non-zero on validation or apply failure.
- Emits execution summary including applied/no-op/failed stages.

## Failure handling

- Partial success is reported with stage-level detail.
- Re-run after failure must reconcile to desired state without manual cleanup where possible.
