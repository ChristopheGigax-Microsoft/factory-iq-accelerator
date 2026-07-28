# ISA-95 Data Generator — Infrastructure

Terraform configuration for the demo infrastructure. Provisions all Azure resources needed to run the ISA-95 data generator and connects it to the Factory IQ Accelerator pipeline.

## Architecture

```
Azure Function App (.NET 10)
  └─→ Azure IoT Hub S1  (isa95-demo-iothub)
        └─→ Fabric Eventstream  [wired manually — see below]
              └─→ TelemetryLanding (Bronze KQL)
                    └─→ Silver tables → Foundry Agents
```

## Resources provisioned

| Resource | Name pattern | Purpose |
|----------|-------------|---------|
| Resource Group | `rg-isa95-demo` | Isolation from main accelerator |
| **IoT Hub** S1 | `isa95-demo-iothub` | Receives ~360 K messages/day from the Function |
| **IoT Hub Device** | `isa95-generator` | Single device; created via `az iot` CLI in post-deploy step |
| **Function App** | `isa95-demo-func-<hex>` | Hosts the timer-triggered generator |
| App Service Plan | `isa95-demo-plan` | Linux Consumption (Y1) |
| Storage Account | `isa95demo<hex>sa` | Required by Functions runtime |
| Application Insights | `isa95-demo-appinsights` | Live metrics + logs |
| Log Analytics Workspace | `isa95-demo-law` | Backing store for App Insights |

## Deploy

### Prerequisites

- Terraform >= 1.6
- Azure CLI authenticated (`az login`)
- `azure-iot` CLI extension (installed automatically by Terraform post-deploy step)
- Azure Functions Core Tools v4 (for code deployment)

### Steps

```bash
cd samples/isa-95-data-generator/infra/terraform

# 1. Copy and fill in values
cp terraform.tfvars.sample terraform.tfvars
# edit terraform.tfvars with your subscription_id and tenant_id

# 2. Initialise and apply
terraform init
terraform apply

# 3. Deploy the Function code (command printed in Terraform outputs)
cd ../../src
func azure functionapp publish <function-app-name> --dotnet-version 10
```

### Switch demo scenario

Change the active scenario without redeploying code:

```bash
# Via Terraform (recommended — tracked in state)
terraform apply -var 'demo_scenario=TemperatureDrift'

# Or directly with az CLI
az functionapp config appsettings set \
  --name <function-app-name> \
  --resource-group rg-isa95-demo \
  --settings DEMO_SCENARIO=QualityExcursion
```

## Wire IoT Hub → Fabric Eventstream

After deploying, add the IoT Hub as a **Custom Source** in the Fabric Eventstream:

1. Open your Fabric workspace → Eventstream → **Edit**
2. **Add source** → **Azure IoT Hub**
3. IoT Hub: `isa95-demo-iothub` — Consumer group: `$Default`
4. The messages flow automatically into `TelemetryLanding` via the existing Eventstream mapping

> The Eventstream Terraform module (`infra/terraform/modules/eventstream`) currently manages the Eventstream item but source wiring is not yet automated via IaC (Fabric API preview limitation). This will be updated when the API stabilises.
