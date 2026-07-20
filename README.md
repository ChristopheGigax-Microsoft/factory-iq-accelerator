<div align="center">

# 🏭 Factory IQ Accelerator

### Industrial AI Platform — from raw Azure subscription to live manufacturing agents in one deploy.

<br/>

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](LICENSE)
[![IaC: Terraform + Bicep](https://img.shields.io/badge/IaC-Terraform%20%7C%20Bicep-623CE4?style=flat-square&logo=terraform)](infra/)
[![Platform: Microsoft Fabric](https://img.shields.io/badge/Platform-Microsoft%20Fabric-00BCF2?style=flat-square)](https://learn.microsoft.com/fabric)
[![Agents: Azure AI Foundry](https://img.shields.io/badge/Agents-Azure%20AI%20Foundry-0078D4?style=flat-square&logo=microsoft-azure)](https://ai.azure.com)
[![SDK: .NET 10](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet)](src/foundry-agents/)
[![Standard: ISA-95](https://img.shields.io/badge/Standard-ISA--95-2e7d32?style=flat-square)](shared/isa95-model/)

<br/>

[**Quick Start**](#-quick-start) · [**Architecture**](#-architecture) · [**Agents**](#-manufacturing-agents) · [**IaC Engines**](#-iac-engines) · [**Docs**](#-documentation)

</div>

---

## The Problem

Manufacturing plants generate enormous volumes of operational data — KPIs, alarms, sensor readings, work orders — but it sits in silos, disconnected from the people who need it to make decisions.

Standing up the full data and AI stack (data platform, ontology, telemetry pipeline, AI agents) takes months, requires deep expertise in multiple technologies, and still ends up inconsistent across plants.

**Factory IQ Accelerator** solves this by providing a fully wired, opinionated, deployment-ready baseline that any team can stamp onto a new plant in minutes.

---

## What It Does

<div align="center">
<img src="docs/assets/factory-iq-logo.png" alt="Factory IQ — Plant dashboard" width="860"/>
</div>

<br/>

In a single deploy, Factory IQ Accelerator provisions:

| Layer | What Gets Built |
|-------|----------------|
| 🏭 **Data Foundation** | Microsoft Fabric capacity, workspace, Eventhouse, KQL database, Eventstream |
| 📐 **Domain Model** | ISA-95-aligned KQL tables, update policies, and plant hierarchy seeding |
| 🔍 **Search & Knowledge** | Azure AI Search, knowledge base, vector index over maintenance/quality docs |
| 🤖 **AI Agents** | 5 manufacturing agents on Azure AI Foundry (C#/.NET 10) |
| 🔗 **Live Connectors** | Fabric Data Agent (MCP), Work IQ (M365 tasks), Foundry IQ (RAG) |
| 📄 **Handoff Contract** | `connection.json` — engine-agnostic output contract for downstream tools |

---

## Architecture

<!-- Architecture diagram — open docs/assets/factory-iq-architecture.drawio in draw.io -->
<div align="center">
<img src="docs/assets/factory-iq-architecture.drawio.png" alt="Factory IQ Architecture" width="960"/>
</div>

---

## Manufacturing Agents

Five production-ready agents, each covering a distinct manufacturing domain:

<!-- Agents screenshot -->
<div align="center">
<img src="docs/assets/screenshot-dashboard.png" alt="Manufacturing Agents" width="860"/>
</div>

<br/>

| Agent | Role | Tools |
|-------|------|-------|
| ⚙️ **Operations** | Monitor OEE, detect performance deviations, explain root causes | Fabric Data Agent · Foundry IQ KB |
| 🔧 **Maintenance** | Correlate alarms, asset history, sensor trends; recommend corrective actions | Fabric Data Agent · Foundry IQ KB · **Work IQ** |
| 🔬 **Quality** | Investigate scrap, SPC drift, batch failures, defect patterns | Fabric Data Agent · Foundry IQ KB · Web IQ |
| 🏢 **Plant Manager** | Summarize plant performance, escalate critical risks, track open actions | Fabric Data Agent · Foundry IQ KB · **Work IQ** |
| 🔁 **Continuous Improvement** | Identify chronic losses, kaizen opportunities, improvement trends | Fabric Data Agent · Foundry IQ KB · Web IQ |

Agents are registered as **versioned Foundry Agents** and stay persistent in the portal between runs.

---

## Connectors & IQ Sources

<!-- PLACEHOLDER: Connector diagram -->
<!-- Image: docs/assets/connectors-diagram.png -->
<!-- Specs: 900×300px, horizontal flow with 4 connector boxes and arrows pointing to "Agents" hub in center -->
<!-- Content: left-to-right: [Fabric Data Agent] → [Agents Hub] ← [Foundry IQ / AI Search] ← [Work IQ] ← [Web IQ] -->

| Connector | Technology | Used By |
|-----------|-----------|---------|
| **Fabric IQ** | Fabric Data Agent MCP — live KQL queries on Eventhouse | All agents |
| **Foundry IQ** | Azure AI Search — RAG over maintenance procedures, quality standards, lean templates | All agents |
| **Work IQ** | Microsoft 365 work management (Planner, Tasks) | Maintenance · Plant Manager |
| **Web IQ** | Bing / web search for benchmarks and supplier specs | Quality · Continuous Improvement |

---

## IaC Engines

Pick one — both produce the same `connection.json` handoff contract.

<table>
<tr>
<th>Terraform</th>
<th>Bicep</th>
</tr>
<tr>
<td>

```bash
terraform -chdir=infra/terraform init
terraform -chdir=infra/terraform apply \
  -var-file=environments/dev.tfvars
terraform -chdir=infra/terraform output \
  -json connection_contract > connection.json
```

</td>
<td>

```bash
az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file infra/bicep/main.bicep \
  --parameters infra/bicep/environments/dev.bicepparam

az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name main \
  --query properties.outputs.connectionContract.value \
  > connection.json
```

</td>
</tr>
</table>

---

## 🚀 Quick Start

### 1. Prerequisites

- Azure CLI authenticated (`az login`)
- Owner access on target subscription
- .NET 10 SDK (for Foundry agents)
- Python 3.11+ (for ISA-95 model scripts)
- Terraform ≥ 1.6 **or** Azure CLI with Bicep

### 2. Set context

```bash
export PLANT_CODE="plant1"
export ENVIRONMENT="dev"
export REGION="westeurope"
export RESOURCE_GROUP="rg-fiq-${PLANT_CODE}-${ENVIRONMENT}"
```

### 3. Deploy infrastructure

```bash
# Terraform path
terraform -chdir=infra/terraform init
terraform -chdir=infra/terraform apply -var-file=environments/dev.tfvars -auto-approve
terraform -chdir=infra/terraform output -json connection_contract > connection.json
```

### 4. Deploy ISA-95 model

```bash
python shared/scripts/deploy-model.py \
  --connection ./connection.json \
  --core-dir ./shared/isa95-model/core \
  --extensions-dir ./shared/isa95-model/extensions \
  --hierarchy-config ./shared/isa95-model/config/plant-hierarchy.yaml
```

### 5. Run an agent

```bash
export PROJECT_ENDPOINT="<from connection.json: foundryProjectEndpoint>"
export FOUNDRY_FABRIC_DATA_AGENT_PROJECT_CONNECTION_NAME="fabric-iq-data-agent-connection"

dotnet run --project src/foundry-agents/agents/FactoryIQ.Agents.Maintenance \
  -- "Show me the top 5 open alarms for line 1"
```

<!-- PLACEHOLDER: Quick start terminal recording -->
<!-- Image: docs/assets/quickstart-terminal.gif or .png -->
<!-- Specs: 1200×500px, dark terminal theme (Dracula / Tokyo Night) -->
<!-- Content: Animated GIF OR static screenshot of `dotnet run` output showing agent response with cited sources -->

---

## Repo Structure

```
factory-iq-accelerator/
├── infra/
│   ├── terraform/          # Terraform: Fabric + Foundry + AI Search + Storage
│   └── bicep/              # Bicep: same stack, ARM-native
├── src/
│   ├── foundry-agents/     # .NET 10 — 5 AI manufacturing agents
│   │   ├── shared/         # FoundryAgentBase, AgentRunner, FoundryConfig
│   │   ├── agents/         # Operations · Maintenance · Quality · PlantManager · CI
│   │   └── knowledge/      # Sample docs for Foundry IQ (maintenance, quality, lean)
│   └── fabric-apps/        # Fabric workspace application
├── shared/
│   └── isa95-model/        # ISA-95 KQL schema, update policies, hierarchy YAML
├── contracts/              # connection.json schema + model runner interface
└── docs/                   # Architecture, Foundry agent integration guide
```

---

## Connection Contract

Every IaC engine emits the same artifact — `connection.json` — used by all downstream tools:

```json
{
  "tenantId": "...",
  "subscriptionId": "...",
  "resourceGroup": "rg-fiq-plant1-dev",
  "region": "westeurope",
  "workspaceId": "...",
  "eventhouseId": "...",
  "kqlDatabase": "fiq-plant1-dev-kql",
  "foundryProjectEndpoint": "https://...",
  "foundryIqProjectConnectionName": "foundry-iq-kb-connection",
  "foundryFabricProjectConnectionName": "fabric-iq-data-agent-connection",
  "foundryWorkIqProjectConnectionName": "work-iq-connection",
  "aiSearchEndpoint": "https://...",
  "modelDeploymentName": "gpt-4o",
  "schemaVersion": "3.0"
}
```

> The contract is engine-blind: swap Terraform for Bicep (or the other way around) without touching any downstream code.

---

## Customization

No forking needed. Use these files as your customization surface:

| File | What To Change |
|------|---------------|
| `shared/isa95-model/config/plant-hierarchy.yaml` | Enterprise → Site → Area → WorkCenter → WorkUnit definitions |
| `shared/isa95-model/extensions/*.kql` | Customer-specific schema, functions, policies |
| `shared/eventstream/definition/eventstream.json` | Telemetry ingestion topology |
| `infra/<engine>/environments/*` | Plant code, region, SKU, optional feature flags |
| `src/foundry-agents/knowledge/` | Maintenance procedures, quality standards, lean templates |

**Rule:** Keep `shared/isa95-model/core/` unchanged. All customer-specific schema goes in `extensions/`.

---

## Documentation

| Document | Description |
|----------|-------------|
| [Architecture](docs/architecture.md) | Platform architecture, design decisions |
| [Foundry Agents](docs/foundry-agents.md) | Agent integration guide, connectors, RBAC |
| [Connection Contract](contracts/connection-contract.md) | Schema reference for `connection.json` |
| [Terraform README](infra/terraform/README.md) | Terraform-specific deployment guide |
| [Bicep README](infra/bicep/README.md) | Bicep-specific deployment guide |
| [ISA-95 Model](shared/isa95-model/README.md) | Model schema, hierarchy, extension guide |
| [Foundry Agents (src)](src/foundry-agents/README.md) | Agent developer guide, env vars, local run |

---

## Tech Stack

| Technology | Role |
|------------|------|
| [Microsoft Fabric](https://learn.microsoft.com/fabric) | Real-time operational data platform |
| [Azure AI Foundry](https://ai.azure.com) | Agent hosting, connections, versioning |
| [Azure OpenAI (GPT-4o)](https://learn.microsoft.com/azure/ai-services/openai/) | Agent reasoning and response generation |
| [Azure AI Search](https://learn.microsoft.com/azure/search/) | Foundry IQ vector / hybrid search |
| [.NET 10 / C#](https://dotnet.microsoft.com) | Agent runtime |
| [Microsoft Agent Framework](https://github.com/microsoft/agents) | Agent orchestration SDK |
| [Terraform](https://www.terraform.io) + [Fabric Provider](https://registry.terraform.io/providers/microsoft/fabric) | IaC engine A |
| [Bicep](https://learn.microsoft.com/azure/azure-resource-manager/bicep/) | IaC engine B |
| [ISA-95](https://www.isa.org/standards-and-publications/isa-standards/isa-standards-committees/isa95) | Manufacturing ontology standard |

---

<div align="center">

Built with ❤️ for manufacturing teams by Microsoft.

<!-- PLACEHOLDER: Microsoft logo or "Made with Azure" badge -->
<!-- Image: docs/assets/made-with-azure.svg -->
<!-- Specs: 200×48px SVG, use the official "Built on Azure" badge SVG from microsoft.com/azure/certifications -->

</div>
