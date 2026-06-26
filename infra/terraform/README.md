# Terraform Engine

## Deploy

```bash
terraform -chdir=infra/terraform init
terraform -chdir=infra/terraform plan -var-file=environments/dev.tfvars
terraform -chdir=infra/terraform apply -var-file=environments/dev.tfvars
```

## Export connection contract

```bash
terraform -chdir=infra/terraform output -json connection_contract > connection.json
```

The generated `connection.json` must conform to `contracts/connection-contract.md`.

## SQL Baseline Rollout Notes

- Extend `connection_contract` output with optional `sqlBaseline` metadata (`server`, `database`, `driver`, `authentication`).
- Keep required v1.0 fields stable to preserve contract compatibility.
- Apply SQL schema scripts in `shared/sql/baseline/` before running SQL-target baseline seed via `shared/scripts/deploy-model.py`.
