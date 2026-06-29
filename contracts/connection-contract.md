# Connection Contract Schema Reference

## Required JSON Shape

```json
{
  "tenantId": "string",
  "subscriptionId": "string",
  "resourceGroup": "string",
  "region": "string",
  "workspaceId": "string",
  "eventhouseId": "string",
  "kqlDatabase": "string",
  "generatedAt": "ISO-8601 string",
  "schemaVersion": "1.0",
  "sqlBaseline": {
    "server": "string",
    "database": "string",
    "driver": "string",
    "authentication": "string"
  }
}
```

## Semantic Rules

- `workspaceId` and `eventhouseId` must identify resources created by the selected engine run.
- `kqlDatabase` must match the database where ISA-95 model scripts are applied.
- `schemaVersion` must be supported by runner implementation.
- `sqlBaseline` is optional additive metadata. When present, the runner seeds ISA-95 baseline nodes to SQL.
- Additional fields are allowed for diagnostics and ignored by runner logic.

## Producer / Consumer

- Producer: Terraform or Bicep deployment outputs.
- Consumer: `shared/scripts/deploy-model.py`.

## Validation Command Examples

```bash
# Check file exists
test -f connection.json

# Validate required keys
jq -e '.tenantId and .subscriptionId and .resourceGroup and .region and .workspaceId and .eventhouseId and .kqlDatabase and .generatedAt and .schemaVersion' connection.json

# Verify schema version
jq -e '.schemaVersion == "1.0"' connection.json
```
