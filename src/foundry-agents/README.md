# Factory IQ — Foundry Agents

AI agents built with the **Azure.AI.Agents.Persistent** SDK (.NET 10) and deployed as persistent assistants in Azure AI Foundry.

## Agents

| Agent | Folder | Role |
|-------|--------|------|
| **Operations** | `agents/FactoryIQ.Agents.Operations` | Monitors plant performance (OEE, availability, quality rate), detects deviations from targets, and explains likely root causes. |
| **Maintenance** | `agents/FactoryIQ.Agents.Maintenance` | Correlates alarms, asset history, work orders, and sensor trends to diagnose equipment issues and recommend corrective actions. |
| **Quality** | `agents/FactoryIQ.Agents.Quality` | Investigates scrap, defects, SPC drift, and batch issues; provides statistical insights and links to quality standards. |
| **Plant Manager** | `agents/FactoryIQ.Agents.PlantManager` | Summarizes overall plant performance, escalates business-critical risks, and tracks open actions. |
| **Continuous Improvement** | `agents/FactoryIQ.Agents.ContinuousImprovement` | Identifies recurring losses, chronic downtime patterns, and improvement opportunities using Lean/TPM frameworks. |

## Connectors & IQ Sources

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
│  ┌───────────┐  ┌───────────┐  ┌───────────┐               │
│  │ Operations│  │Maintenance│  │  Quality  │  ...           │
│  │   Agent   │  │   Agent   │  │   Agent   │               │
│  └─────┬─────┘  └─────┬─────┘  └─────┬─────┘               │
│        │               │               │                    │
│        ▼               ▼               ▼                    │
│  ┌──────────────────────────────────────────┐               │
│  │        Function Tool Dispatch            │               │
│  └──────┬──────────────┬───────────┬────────┘               │
│         │              │           │                        │
└─────────┼──────────────┼───────────┼────────────────────────┘
          │              │           │
          ▼              ▼           ▼
  ┌──────────────┐ ┌──────────┐ ┌──────────┐
  │ Fabric Data  │ │ AI Search│ │ Web IQ / │
  │    Agent     │ │(Foundry  │ │ Work IQ  │
  │  (KQL/SQL)   │ │   IQ)    │ │          │
  └──────────────┘ └──────────┘ └──────────┘
          │
          ▼
  ┌──────────────┐
  │  Microsoft   │
  │    Fabric    │
  │  Lakehouse   │
  └──────────────┘
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Azure CLI (`az login` to the correct tenant)
- Deployed infrastructure (see `infra/terraform/` or `infra/bicep/`)

### Environment Variables

```bash
export PROJECT_ENDPOINT="https://<foundry>.services.ai.azure.com/api/projects/<project>"
export AI_SEARCH_ENDPOINT="https://<search>.search.windows.net"
export STORAGE_ACCOUNT_ENDPOINT="https://<storage>.blob.core.windows.net/"
export MODEL_DEPLOYMENT_NAME="gpt-4o"
export AZURE_TENANT_ID="<your-tenant-id>"

# Optional
export FABRIC_DATA_AGENT_ID="<fabric-data-agent-guid>"
export FABRIC_WORKSPACE_ID="<fabric-workspace-guid>"
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
│       ├── Agents/                 # PersistentAgentBase, AgentConsoleHost, AgentRunner
│       ├── Services/               # FabricDataAgent, KnowledgeSearch, ServiceRegistration
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

By default, agents **remain registered** in Azure AI Foundry after the process exits. This allows them to be visible and testable from the Foundry portal. Set `DELETE_PERSISTENT_AGENT_ON_EXIT=true` for ephemeral dev/test usage.

When re-launched, agents detect their existing registration by name and reuse it (no duplicates).
