# SQL Baseline Schema

This folder contains SQL scripts for ISA-95 baseline persistence.

## Script order

1. `010_core_tables.sql` - baseline hierarchy and operational records.
2. `020_audit_tables.sql` - immutable change log and seed run tracking.
3. `030_constraints.sql` - integrity and optimistic concurrency constraints.

## Apply order

Apply scripts in lexical order and within a transaction when supported:

```bash
sqlcmd -S <server> -d <database> -i 010_core_tables.sql
sqlcmd -S <server> -d <database> -i 020_audit_tables.sql
sqlcmd -S <server> -d <database> -i 030_constraints.sql
```

## Troubleshooting

- `FK violation on parent_node_id`: validate hierarchy parent type order before writes.
- `UQ_baseline_node_external_key`: rerun used a different identifier for the same logical node.
- `version conflict`: the API write payload must include the latest `version` value.
