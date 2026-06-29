# ISA-95 Model Structure

- `core/`: project-owned baseline ISA-95 model scripts.
- `extensions/`: customer-owned extensions loaded after core.
- `config/`: plant hierarchy configuration used for seed operations.

## Customization Workflow

1. Add extension scripts under `extensions/` using numeric ordering.
2. Edit `config/plant-hierarchy.yaml` for plant-specific hierarchy only.
3. Run `shared/scripts/deploy-model.py` with the same `connection.json`.

Core files remain unchanged during customization and upgrades.

## SQL Baseline Target Notes

- `shared/scripts/deploy-model.py` now supports SQL baseline seeding when `connection.json` includes `sqlBaseline` metadata.
- SQL schema artifacts are maintained in `shared/sql/baseline/` and should be applied before first SQL-target seed run.
