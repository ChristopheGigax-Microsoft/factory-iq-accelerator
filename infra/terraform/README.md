# Terraform Engine

## Recommended walkthrough

Run the interactive deployment assistant from the repository root:

```powershell
pwsh ./scripts/deploy.ps1
```

The walkthrough always completes scope selection, permissions analysis,
variable collection, preflight, and `terraform plan`. It prints a summary and
then asks whether to apply that exact saved plan. Answering no exits without
deploying anything. There is deliberately no CI or silent-approval mode.

### Deployment scopes

| Scope | Resources |
|-------|-----------|
| Fabric only | Optional Fabric capacity/workspace, Eventhouse, Eventstream, Data Agent, Ontology |
| Fabric + Foundry | Fabric scope plus Foundry/AI Services, chat and embedding models, AI Search, Storage, RBAC, and project connections |
| Agents | Optional application phase after Fabric + Foundry; registers all five prompt agents without opening their REPL |

`enable_foundry = false` selects Fabric only. Work IQ and agent registration
are valid only with Foundry.

### Permissions analysis

The assistant checks what can be established safely before deployment:

- Azure identity, tenant, subscription, inherited RBAC, providers, policies,
  and visible deny assignments;
- direct Fabric workspace access and role when an existing workspace is used;
- Foundry quota visibility for the selected region;
- the signed-in user's object ID when agent publishing is requested.

Results are classified as `OK`, `MISSING`, or `NOT VERIFIABLE`. Missing rights
block before Terraform. Tenant settings, PIM state, transitive permissions,
Entra/Work IQ consent, conditional access, and exact model availability can
require manual confirmation because they cannot always be proven read-only.

### Collected variables

The assistant lists the variables for the selected path, then asks for them one
at a time. Common values are tenant, subscription, region, resource group,
plant code, environment, workspace strategy, and optional routing profile.
Creating a workspace additionally requires a Fabric SKU and capacity admins.
Foundry adds model capacities; Work IQ adds its endpoint and delegated scope.

No password or client secret is requested. Authentication uses the active Azure
CLI identity, managed identities, and Terraform-managed credentials where
required.

## Deploy

The manual path remains available:

```bash
terraform -chdir=infra/terraform init
terraform -chdir=infra/terraform plan -var-file=environments/dev.tfvars
terraform -chdir=infra/terraform apply -var-file=environments/dev.tfvars
```

Important scope variables:

```hcl
create_fabric_workspace = false # Set true to create capacity + workspace
workspace_id            = "..." # Required when the value above is false
enable_foundry          = true  # Set false for Fabric only
enable_work_iq_connection = false
agent_deployer_principal_id = "" # Set by the walkthrough when publishing agents
```

Changing `enable_foundry` from `true` to `false` in an existing state proposes
removal of Foundry and its Azure AI dependencies. Always inspect the plan.

The walkthrough isolates Terraform state in a workspace named
`fiq-<plant_code>-<environment>`. Deployments for different plants or
environments therefore cannot reuse or destroy resources tracked by another
walkthrough deployment.

After planning, the walkthrough prints creation, update, replacement, and
deletion counts directly in the console. Destructive resource addresses are
always listed explicitly. Terraform output is streamed live, and failures stop
the walkthrough with a visible error section before any apply can continue.

Foundry child operations are serialized as account, project, chat model, then
embedding model because Azure AI Services accepts only one concurrent write on
the parent account. The Foundry project also retries transient
`RequestConflict` responses with bounded backoff.

The default deployment creates only the `RawTelemetry` Bronze table. To deploy
customer-owned Silver routes, set the optional profile path:

```hcl
routing_profile_path = "../../customer-routing/routes.json"
```

The Fabric Data Agent is deployed with `RawTelemetry` selected, its Eventhouse
columns available to NL2KQL, and Factory IQ instructions in both draft and
published stages. The published definition is immediately available to the
Fabric Data Agent MCP endpoint without a manual portal publish step.

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
