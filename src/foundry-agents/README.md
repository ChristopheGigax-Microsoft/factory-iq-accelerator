# Factory IQ — Foundry Agents

AI agents built with **Azure.AI.Projects** and **Microsoft.Agents.AI.Foundry** (.NET 10), targeting the **new Foundry Agent Service** and the **new Azure AI Foundry portal**.

## Agents

| Agent | Folder | Role |
|-------|--------|------|
| **Operations** | `agents/FactoryIQ.Agents.Operations` | Monitors plant performance (OEE, availability, quality rate), detects deviations from targets, and explains likely root causes. |
| **Maintenance** | `agents/FactoryIQ.Agents.Maintenance` | Correlates alarms, asset history, work orders, and sensor trends to diagnose equipment issues and recommend corrective actions. |
| **Quality** | `agents/FactoryIQ.Agents.Quality` | Investigates scrap, defects, SPC drift, and batch issues; provides statistical insights and links to quality standards. |
| **Plant Manager** | `agents/FactoryIQ.Agents.PlantManager` | Summarizes overall plant performance, escalates business-critical risks, and tracks open actions. |
| **Continuous Improvement** | `agents/FactoryIQ.Agents.ContinuousImprovement` | Identifies recurring losses, chronic downtime patterns, and improvement opportunities using Lean/TPM frameworks. |

## Connectors & IQ Sources

> Current status: these connectors are the **target architecture** and are **not wired yet** in code. The agents are currently prompt-based Foundry agents, and connectors will be added incrementally.

| Connector | Purpose | Used by |
|-----------|---------|---------|
| **Fabric Data Agent** | Queries live operational data from Microsoft Fabric (KQL, SQL) — OEE, alarms, work orders, sensor readings | All agents |
| **Foundry IQ (AI Search)** | RAG over indexed knowledge base — maintenance procedures, quality standards, Lean templates | All agents |
| **Work IQ** | Surfaces relevant work items, tasks, and action plans from connected work management systems | Maintenance, Plant Manager |
| **Web IQ** | Searches external web sources for benchmarks, supplier data, and industry standards | Quality, Continuous Improvement |

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Azure AI Foundry (v2)                     │
│  ┌───────────────────────────────────────────────────────┐  │
│  │     Versioned Prompt Agents (new Foundry portal)     │  │
│  │ Operations • Maintenance • Quality • Plant Manager   │  │
│  │ Continuous Improvement                               │  │
│  └───────────────────────────┬───────────────────────────┘  │
│                              │                              │
└──────────────────────────────┼──────────────────────────────┘
                               │
                               ▼
                 ┌─────────────────────────────┐
                 │     Future connector layer  │
                 │ Fabric Data • Foundry IQ    │
                 │ Work IQ • Web IQ            │
                 └─────────────────────────────┘
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Azure CLI (`az login` to the correct tenant)
- Deployed infrastructure (see `infra/terraform/` or `infra/bicep/`)

### Environment Variables

```bash
export PROJECT_ENDPOINT="https://<foundry>.services.ai.azure.com/api/projects/<project>"
export MODEL_DEPLOYMENT_NAME="gpt-4o"
export AZURE_TENANT_ID="<your-tenant-id>"

# Optional
export DELETE_PERSISTENT_AGENT_ON_EXIT="false"  # default: agents stay persistent
```

### Run an Agent

```bash
# One-shot query
dotnet run --project agents/FactoryIQ.Agents.Operations -- "What is the current OEE for line 1?"

# Interactive REPL
dotnet run --project agents/FactoryIQ.Agents.Maintenance
```

### Build All

```bash
dotnet build FactoryIQ.Agents.slnx
```

## Project Structure

```
src/foundry-agents/
├── FactoryIQ.Agents.slnx          # Solution file
├── shared/
│   └── FactoryIQ.Agents.Shared/   # Base classes, DI, services
│       ├── Agents/                 # FoundryAgentBase, AgentConsoleHost
│       ├── Services/               # AIProjectClient registration, AgentRunner
│       └── Models/                 # FoundryConfig, domain models
├── agents/
│   ├── FactoryIQ.Agents.Operations/
│   ├── FactoryIQ.Agents.Maintenance/
│   ├── FactoryIQ.Agents.Quality/
│   ├── FactoryIQ.Agents.PlantManager/
│   └── FactoryIQ.Agents.ContinuousImprovement/
└── knowledge/                      # Static knowledge base documents
    ├── maintenance-procedures/
    ├── quality-standards/
    └── lean-templates/
```

## Persistence

By default, agents **remain registered** in Azure AI Foundry after the process exits. This allows them to stay visible in the new Foundry portal as **versioned Foundry agents**. Set `DELETE_PERSISTENT_AGENT_ON_EXIT=true` for ephemeral dev/test usage.

When re-launched, agents detect the latest server-managed version by name and reuse it when the definition matches. When the local definition changes, a **new agent version** is published automatically.
