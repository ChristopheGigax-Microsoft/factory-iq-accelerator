# Terraform Engine

## Deploy

```bash
terraform -chdir=infra/terraform init
terraform -chdir=infra/terraform plan -var-file=environments/dev.tfvars
terraform -chdir=infra/terraform apply -var-file=environments/dev.tfvars
```

The default deployment creates only the `RawTelemetry` Bronze table. To deploy
customer-owned Silver routes, set the optional profile path:

```hcl
routing_profile_path = "../../customer-routing/routes.json"
```

See [`docs/routing-profiles.md`](../../docs/routing-profiles.md) for the profile
format and the included ISA-95 demo profile.

## Export connection contract

```bash
terraform -chdir=infra/terraform output -json connection_contract > connection.json
```

The generated `connection.json` must conform to `contracts/connection-contract.md`.

## SQL Baseline Rollout Notes

- Extend `connection_contract` output with optional `sqlBaseline` metadata (`server`, `database`, `driver`, `authentication`).
- Keep required v1.0 fields stable to preserve contract compatibility.
- Apply SQL schema scripts in `shared/sql/baseline/` before running SQL-target baseline seed via `shared/scripts/deploy-model.py`.
