# ISA-95 demo routing profile

This optional profile maps events from
`samples/isa-95-data-generator` into the repository's demonstration tables:

- `EquipmentTelemetry`
- `EquipmentActual`
- `WorkRequest`
- `WorkResponse`
- `MaterialActual`
- `QualityTestResult`

Enable it in the main Terraform configuration:

```hcl
routing_profile_path = "../../samples/routing/isa95-demo/routes.json"
```

The profile reflects the generator's event contract. It is not deployed by
default and is not intended as a universal ISA-95 schema.

Validate it without connecting to Fabric:

```powershell
python shared\scripts\deploy-routing-profile.py `
  --profile samples\routing\isa95-demo\routes.json `
  --dry-run
```
