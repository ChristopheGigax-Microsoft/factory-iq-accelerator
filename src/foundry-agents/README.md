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

> Current status: Foundry IQ (knowledge base MCP) and Fabric OneLake Catalog (Fabric Data Agent via Fabric IQ) are wired in the agents. Work IQ and Web IQ remain the next connectors to be added incrementally.

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
                 │      Connector layer        │
                 │ Fabric Data • Foundry IQ    │
                 │ Work IQ • Web IQ            │
                 └─────────────────────────────┘
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Azure CLI (`az login` to the correct tenant)
- Deployed infrastructure (see `infra/terraform/` or `infra/bicep/`)
- Published Fabric Data Agent (required for Fabric IQ MCP tools to resolve)

### Environment Variables

```bash
export PROJECT_ENDPOINT="https://<foundry>.services.ai.azure.com/api/projects/<project>"
export MODEL_DEPLOYMENT_NAME="gpt-4o"
export AZURE_TENANT_ID="<your-tenant-id>"
export AI_SEARCH_ENDPOINT="https://<search>.search.windows.net"
export FOUNDRY_IQ_KNOWLEDGE_BASE_NAME="<search-knowledge-base-name>"
export FOUNDRY_IQ_PROJECT_CONNECTION_NAME="foundry-iq-kb-connection"
export FOUNDRY_FABRIC_DATA_AGENT_PROJECT_CONNECTION_NAME="fabric-iq-data-agent-connection"

# Optional
export DELETE_PERSISTENT_AGENT_ON_EXIT="false"  # default: agents stay persistent
export USE_MANAGED_IDENTITY="false"             # default local run: false; set true in Azure-hosted runtime
```

`FOUNDRY_FABRIC_DATA_AGENT_PROJECT_CONNECTION_NAME` can be either the project connection **name** or its full ARM **resource ID**.

### Fabric Data Agent readiness check

Before running agents that use Fabric IQ, verify MCP tool discovery works on the Data Agent endpoint:

```bash
TOKEN=$(az account get-access-token --resource https://api.fabric.microsoft.com --query accessToken -o tsv)
curl -X POST "<fabric_data_agent_mcp_target>" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'
```

If `tools/list` fails with HTTP 404, publish/re-publish the Fabric Data Agent before retesting Foundry agents.

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

When re-launched, agents detect the latest server-managed version by name and reuse it when the definition matches (including MCP Foundry IQ tool configuration). When the local definition changes, a **new agent version** is published automatically.
