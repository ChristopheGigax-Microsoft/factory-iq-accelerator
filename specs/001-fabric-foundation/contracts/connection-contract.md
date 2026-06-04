# Contract: connection.json

Purpose: Standard deployment handoff consumed by model deployment tooling independent of IaC engine.

## Required shape

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
  "schemaVersion": "1.0"
}
```

## Rules

- Producer: Terraform, Bicep, or Pulumi deployment output.
- Consumer: model runner only.
- Engine must not alter logical field semantics.
- Unknown extra fields are allowed for diagnostics but ignored by model runner.
- Missing required fields are a hard validation error.

## Validation checklist

- Contract file exists after successful deploy.
- JSON is syntactically valid.
- All required fields are present and non-empty.
- `schemaVersion` matches supported runner schema.
- Resource identifiers point to the deployed plant context.
