# ISA-95 Model Structure

- `core/`: reference hierarchy definitions. Operational Silver tables are
  customer-owned routing-profile outputs.
- `extensions/`: customer-owned extensions loaded after core.
- `config/`: plant hierarchy configuration used for seed operations.

## Customization Workflow

1. Add extension scripts under `extensions/` using numeric ordering.
2. Edit `config/plant-hierarchy.yaml` for plant-specific hierarchy only.
3. Run `shared/scripts/deploy-model.py` with the same `connection.json`.

Core files remain unchanged during customization and upgrades.

The accelerator deploys only the `RawTelemetry` Bronze table by default. See
[`docs/routing-profiles.md`](../../docs/routing-profiles.md) to create optional
Silver tables. The repository's executable ISA-95 example is under
`samples/routing/isa95-demo/`.

## SQL Baseline Target Notes

- `shared/scripts/deploy-model.py` now supports SQL baseline seeding when `connection.json` includes `sqlBaseline` metadata.
- SQL schema artifacts are maintained in `shared/sql/baseline/` and should be applied before first SQL-target seed run.
