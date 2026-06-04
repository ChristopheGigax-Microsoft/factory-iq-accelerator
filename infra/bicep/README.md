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
