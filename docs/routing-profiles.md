# Customer routing profiles

Factory IQ always deploys a `RawTelemetry` Bronze table. Silver tables are
optional and customer-owned: a routing profile declares their schemas and the
KQL transformations that populate them.

## Bronze contract

All sources must be mapped by Eventstream to this stable envelope:

```kusto
RawTelemetry (
  IngestedAt:datetime,
  EventTime:datetime,
  SourceSystem:string,
  SourceSchema:string,
  SourceRecordId:string,
  Payload:dynamic
)
```

`Payload` contains the complete source event. `EventTime` is the source event
time; `IngestedAt` records when the producer created the ingestion envelope.
`SourceRecordId` should be stable and unique enough for replay and
deduplication.

## Enable a profile

Set the optional Terraform variable before deployment:

```hcl
routing_profile_path = "../../customer-routing/routes.json"
```

Leave it unset or empty for a Bronze-only deployment:

```hcl
routing_profile_path = ""
```

Paths are resolved from the directory where Terraform runs. An absolute path is
also accepted.

The repository includes an executable example based on the ISA-95 demo
generator:

```hcl
routing_profile_path = "../../samples/routing/isa95-demo/routes.json"
```

The example is a demonstration, not a universal ISA-95 schema.

## Profile layout

```text
customer-routing/
├── routes.json
└── transforms/
    ├── production.kql
    └── quality.kql
```

`routes.json` declares the source, target tables, target schemas, and transform
files:

```json
{
  "version": 1,
  "sourceTable": "RawTelemetry",
  "routes": [
    {
      "name": "production",
      "targetTable": "ProductionEvent",
      "enabled": true,
      "transactional": false,
      "columns": [
        { "name": "EventTime", "type": "datetime" },
        { "name": "MaterialId", "type": "string" },
        { "name": "Quantity", "type": "real" }
      ],
      "transformFile": "transforms/production.kql"
    }
  ]
}
```

Each transform is a query that starts from `RawTelemetry`, filters matching
events, and projects columns in the exact name, type, and order declared by the
route:

```kusto
RawTelemetry
| where tostring(Payload.reportType) == "Production.Report"
| project
    EventTime,
    MaterialId = tostring(Payload.materialId),
    Quantity = todouble(Payload.quantity)
```

One source event may populate multiple target tables when multiple transforms
match it.

## Deployment behavior

For each route, Terraform invokes
`shared/scripts/deploy-routing-profile.py`, which:

1. validates the profile and transform paths;
2. creates or merges the target table;
3. creates an `fn_route_<name>` KQL function;
4. installs an update policy from `RawTelemetry` to the target.

The deployer rejects invalid identifiers, unsupported Kusto types, duplicate
routes, transforms outside the profile directory, and transforms containing
management commands.

Validate a profile locally without connecting to Fabric:

```powershell
python shared\scripts\deploy-routing-profile.py `
  --profile samples\routing\isa95-demo\routes.json `
  --dry-run
```

## Operational rules

- Keep policies nontransactional unless Bronze ingestion must fail when a
  Silver transformation fails. The default `false` protects raw-data capture.
- Update policies process new ingestions only. Correcting a rule does not
  automatically backfill existing Bronze rows.
- Test a corrected function against a bounded `IngestedAt` range before an
  explicit `.set-or-append` backfill.
- Set `enabled` to `false` and apply once before removing a route. Simply
  deleting it from the manifest cannot remove a policy that was deployed by an
  earlier profile version.
- Disabling or removing a route never deletes its table or historical data.
  Destructive cleanup remains an explicit customer operation.
- Source and target tables must remain in the same KQL database.
- A transform's output names, types, and order must exactly match its declared
  target schema.
- `.create-merge` safely adds missing columns but does not replace incompatible
  existing columns. Treat breaking Silver schema changes as explicit
  migrations.
- Each additional route adds ingestion work; keep transforms selective and
  inexpensive.

## Existing deployments

Terraform now creates `RawTelemetry` and points the managed Eventstream at it.
Upgrading never drops tables or historical data left over from an earlier
deployment, so review and remove any obsolete tables and policies manually
after validating the new ingestion path.

For the platform limitation and update-policy semantics, see the
[Microsoft Fabric update policy documentation](https://learn.microsoft.com/kusto/management/update-policy?view=microsoft-fabric).
