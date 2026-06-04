# Model Runner Interface

## CLI

```bash
python shared/scripts/deploy-model.py \
  --connection ./connection.json \
  --core-dir ./shared/isa95-model/core \
  --extensions-dir ./shared/isa95-model/extensions \
  --hierarchy-config ./shared/isa95-model/config/plant-hierarchy.yaml
```

## Inputs

- `--connection`: path to deployment handoff contract.
- `--core-dir`: folder containing core KQL scripts.
- `--extensions-dir`: folder containing customer extension KQL scripts.
- `--hierarchy-config`: hierarchy YAML used for seed operations.
- `--fail-on-warning` (optional): fail run on warnings.

## Stage Order

1. Validate connection contract.
2. Apply core scripts in lexical order.
3. Apply extensions in lexical order.
4. Run hierarchy seeding.

## Exit Codes

- `0`: success or no-op idempotent rerun.
- non-zero: validation/apply/seed failure.

## Failure Triage and Rerun Guidance

1. Validate `connection.json` required fields and region/resource identifiers.
2. Run hierarchy validator directly:
  `python shared/scripts/seed-hierarchy.py --config shared/isa95-model/config/plant-hierarchy.yaml`
3. Verify core scripts run before extension scripts (lexical order).
4. Fix input or script errors and rerun full deployment command.

Reruns are expected and supported; runner behavior must converge without manual cleanup where possible.
