# Quickstart — Fabric Foundation Baseline

## 1) Select one IaC engine

Choose exactly one: Terraform, Bicep, or Pulumi.

- Keep the selected engine folder under `infra/`.
- Remove the two non-selected engine folders to validate isolation.

## 2) Prepare deployment inputs

Set required inputs:
- `plantCode`
- `environment`
- `region`
- `capacitySku` (default recommended: `F2`)

Ensure deterministic naming follows `fiq-{plantCode}-{environment}-{resource}`.

## 3) Authenticate

Use one approved method:
- Service Principal (bootstrap-friendly)
- Managed Identity (preferred production posture)

Do not place secrets in repository files.

## 4) Deploy foundation

Run the engine-specific deployment command for the selected engine.

Expected outcome:
- Capacity
- Workspace
- Eventhouse + KQL database
- Eventstream
- Generated `connection.json`

## 5) Validate contract

Verify `connection.json` against [contracts/connection-contract.md](contracts/connection-contract.md).

Required checks:
- JSON validity
- Required fields present
- Field values map to deployed resources

## 6) Deploy model

Invoke model runner with:
- `connection.json`
- shared core model scripts
- customer extension scripts
- `plant-hierarchy.yaml`

Expected outcome:
- ISA-95 core schema created
- Extensions applied
- Plant hierarchy seeded

## 7) Verify idempotency

Re-run deployment and model runner with unchanged inputs.

Success condition:
- No unintended resource duplication
- No duplicate schema artifacts
- Run completes with convergent/no-op behavior

Suggested sequence:

```bash
# Re-run infrastructure apply for selected engine
terraform -chdir=infra/terraform apply -var-file=environments/dev.tfvars

# Re-run model deployment
python shared/scripts/deploy-model.py \
	--connection ./connection.json \
	--core-dir ./shared/isa95-model/core \
	--extensions-dir ./shared/isa95-model/extensions \
	--hierarchy-config ./shared/isa95-model/config/plant-hierarchy.yaml
```

Expected rerun output includes stage completion without new object creation errors.

## 8) Customize safely

Customer customization path:
- Add or update extension scripts in `shared/isa95-model/extensions/`
- Edit only `plant-hierarchy.yaml` for plant structure changes
- Do not edit core model files for customer-specific logic

## 9) Quick validation command sequence

Run these checks after deployment:

```bash
# Validate Terraform syntax (if using Terraform)
terraform -chdir=infra/terraform validate

# Validate Bicep syntax (if using Bicep)
az bicep build --file infra/bicep/main.bicep

# Validate Pulumi program structure (if using Pulumi)
pulumi -C infra/pulumi preview

# Validate hierarchy and run model deployment
python shared/scripts/seed-hierarchy.py --config shared/isa95-model/config/plant-hierarchy.yaml
python shared/scripts/deploy-model.py \
	--connection ./connection.json \
	--core-dir ./shared/isa95-model/core \
	--extensions-dir ./shared/isa95-model/extensions \
	--hierarchy-config ./shared/isa95-model/config/plant-hierarchy.yaml
```
