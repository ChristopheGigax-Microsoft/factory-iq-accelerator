# Bicep Engine

## Deploy

```bash
az deployment group create \
  --resource-group rg-fiq-plant1-dev \
  --template-file infra/bicep/main.bicep \
  --parameters infra/bicep/environments/dev.bicepparam
```

## Output Contract

Capture deployment outputs and materialize `connection.json` using the `connectionContract` output.
The generated artifact must conform to `contracts/connection-contract.md`.

## SQL Baseline Rollout Notes

- Extend `connectionContract` output with optional `sqlBaseline` metadata (`server`, `database`, `driver`, `authentication`).
- Keep all required v1.0 fields unchanged to preserve backward compatibility.
- Apply SQL schema scripts in `shared/sql/baseline/` before running `shared/scripts/deploy-model.py` with SQL target enabled.
