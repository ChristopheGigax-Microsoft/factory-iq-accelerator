# Foundry Agents for Manufacturing

This document describes the AI Foundry agents that extend the Factory IQ Accelerator with intelligent manufacturing capabilities.

## Overview

Five specialized agents are deployed as Azure AI Foundry agents, each targeting a specific manufacturing domain. All agents are built with the **Microsoft Agent Framework** (C#/.NET 10) and are registered in a shared Foundry project connected to the plant's Fabric workspace.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Azure AI Foundry Project                   │
│                                                              │
│  ┌────────────┐  ┌────────────┐  ┌────────────────────────┐│
│  │ Operations │  │Maintenance │  │   Quality Agent        ││
│  │   Agent    │  │   Agent    │  │                        ││
│  └─────┬──────┘  └─────┬──────┘  └─────┬──────────────────┘│
│  ┌─────┴──────┐  ┌─────┴──────┐                             │
│  │   Plant    │  │Continuous  │                              │
│  │  Manager   │  │Improvement │                              │
│  └────────────┘  └────────────┘                              │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │                   Tool Connections                     │   │
│  │  • Fabric Data Agent (KQL telemetry queries)          │   │
│  │  • Fabric IQ (semantic model for OEE/KPIs)           │   │
│  │  • Foundry IQ / AI Search (document grounding)        │   │
│  │  • Work IQ (work orders & tasks)                      │   │
│  │  • Web IQ (external lookups)                          │   │
│  └──────────────────────────────────────────────────────┘   │
└──────────────────────────────────┬──────────────────────────┘
                                   │
              ┌────────────────────┼────────────────────┐
              │                    │                    │
        ┌─────┴─────┐     ┌──────┴──────┐     ┌──────┴──────┐
        │  Fabric   │     │   Azure AI  │     │   Storage   │
        │  KQL DB   │     │   Search    │     │   Account   │
        └───────────┘     └─────────────┘     └─────────────┘
```

## Fabric ↔ Foundry Integration

There are **two primary integration paths** between Microsoft Fabric and Azure AI Foundry:

### 1. Fabric Data Agent (Connector)

The Fabric Data Agent is already provisioned in the Fabric workspace (via IaC). Foundry agents call it through **Fabric IQ (OneLake Catalog)** to run natural-language queries that are translated into KQL against the Eventhouse. This path uses On-Behalf-Of (OBO) authorization.

This accelerator intentionally keeps Foundry connected to the **Data Agent endpoint only** (`fabric-iq-data-agent-connection`), not directly to an ontology endpoint.

### 2. Fabric IQ (Semantic Layer)

Fabric IQ is the semantic intelligence layer that provides ontology-grounded access to Fabric data. It ensures agents interpret data consistently using shared business definitions (entities, relationships, metrics). This prevents hallucination and provides reliable, contextual answers.

When a Fabric Ontology is enabled, it must be added as a **data source inside the Fabric Data Agent**. Foundry integration remains unchanged.

## Agents

### Operations Agent

| Aspect | Detail |
|--------|--------|
| **Purpose** | Monitor plant performance, detect deviations, explain likely causes |
| **Tools** | Fabric Data Agent (KQL telemetry), Fabric IQ (OEE semantic model), Foundry IQ (OEE playbooks) |
| **Project** | `src/foundry-agents/agents/FactoryIQ.Agents.Operations/` |

### Maintenance Agent

| Aspect | Detail |
|--------|--------|
| **Purpose** | Correlate alarms, asset history, work orders, and sensor trends |
| **Tools** | Fabric Data Agent (sensor KQL), Foundry IQ (maintenance procedures, runbooks), Work IQ (work orders) |
| **Project** | `src/foundry-agents/agents/FactoryIQ.Agents.Maintenance/` |

### Quality Agent

| Aspect | Detail |
|--------|--------|
| **Purpose** | Investigate scrap, defects, process drift, batch issues |
| **Tools** | Fabric Data Agent (quality KQL), Foundry IQ (quality standards, SPC docs), Web IQ (supplier specs) |
| **Project** | `src/foundry-agents/agents/FactoryIQ.Agents.Quality/` |

### Plant Manager Agent

| Aspect | Detail |
|--------|--------|
| **Purpose** | Summarize plant performance, escalate business-critical risks |
| **Tools** | Fabric Data Agent (KPI queries), Fabric IQ (aggregated semantic model), Foundry IQ (escalation playbooks), Work IQ (open items) |
| **Project** | `src/foundry-agents/agents/FactoryIQ.Agents.PlantManager/` |

### Continuous Improvement Agent

| Aspect | Detail |
|--------|--------|
| **Purpose** | Identify recurring losses and improvement opportunities |
| **Tools** | Fabric Data Agent (historical KQL), Foundry IQ (lean/kaizen templates), Web IQ (industry benchmarks) |
| **Project** | `src/foundry-agents/agents/FactoryIQ.Agents.ContinuousImprovement/` |

## Knowledge Base (Foundry IQ)

Sample documents are provided in `src/foundry-agents/knowledge/` for indexing into Azure AI Search:

| Folder | Content | Used By |
|--------|---------|---------|
| `maintenance-procedures/` | PM procedures, alarm correlation guides, runbooks | Maintenance, Operations |
| `quality-standards/` | SPC guides, inspection criteria, defect references | Quality |
| `lean-templates/` | Kaizen templates, TPM guides, improvement frameworks | Continuous Improvement |

The customer uploads these documents to the Storage Account's `knowledge-base` container. Terraform provisions the Azure AI Search knowledge source and knowledge base on top of that container; file upload is intentionally out of scope for IaC. The blob knowledge source uses an Azure OpenAI embedding deployment hosted on the Foundry resource so Azure AI Search can build a vector index for semantic retrieval.

In addition, IaC now creates a **Foundry project connection** (`foundry-iq-kb-connection`) pointing to the knowledge base MCP endpoint:

`https://<search>.search.windows.net/knowledgebases/<kb-name>/mcp?api-version=2026-05-01-preview`

Each Factory IQ agent is registered with an MCP knowledge base tool (`knowledge_base_retrieve`) so responses can be grounded directly on Foundry IQ content.

IaC also creates a **Fabric IQ project connection** (`fabric-iq-data-agent-connection`) in the same shape as the Foundry portal connector:

- `category: RemoteTool`
- `authType: UserEntraToken`
- `audience: https://api.fabric.microsoft.com`
- `metadata.type: fabric_iq_preview`
- `target: <Fabric Data Agent MCP endpoint>`

For tenants requiring a region-scoped Fabric MCP endpoint, Terraform exposes `fabric_data_agent_mcp_target` to pass the exact portal-discovered URL.

Each Factory IQ agent is registered with the **Fabric OneLake Catalog** tool (`fabric_iq_preview`) bound to this connection so agents can query live Fabric data.

## Fabric Ontology in this accelerator

Ontology integration is supported using this pattern:

1. Create/refine Ontology in Fabric (preview).
2. Add Ontology as an additional source in the Fabric Data Agent.
3. Publish Data Agent.
4. Keep Foundry agents connected to the same Data Agent MCP endpoint.

Use:

- `docs/fabric-ontology.md` for implementation steps.
- `shared/ontology/factory-iq-ontology-blueprint.yaml` for the proposed ISA-95 ontology model.

IaC also creates an optional **Work IQ MCP project connection** (`work-iq-connection`) when `enable_work_iq_connection = true`. Work IQ only supports **bring-your-own Entra app (On-Behalf-Of)** authentication — there is no shared/first-party app option — so Terraform owns the full lifecycle:

1. **`module.workiq_app`** (`infra/terraform/modules/workiq_app/`) registers a dedicated confidential-client Entra app + service principal, grants the `WorkIQAgent.Ask` delegated permission, and issues a client secret.
2. **`azapi_resource.work_iq_connection`** creates the Foundry project connection as an OAuth2 MCP connection:
   - `category: RemoteTool`
   - `group: ServicesAndApps` (OAuth2 fields — `TokenUrl`/`AuthorizationUrl`/`RefreshUrl`/`Scopes`/`Credentials` — must live at the top level of `properties`, **not** nested under `metadata`, or Foundry silently drops them and the MCP call fails with a 401 "Access token is empty")
   - `metadata.type: work_iq_mcp`
   - `target: https://workiq.svc.cloud.microsoft/mcp` (`var.work_iq_mcp_endpoint`)
   - `credentials`: the app's client ID/secret from `module.workiq_app`
3. **`null_resource.work_iq_redirect_uri`** closes the loop: Foundry only generates the connection's OAuth `redirectUrl` once the connection itself exists, so the Entra app can't declare it up front. This resource runs `az ad app update --web-redirect-uris <redirectUrl>` via `local-exec`, triggered on the app's client ID + the connection's exported `properties.redirectUrl`, so the app's Web redirect URI stays in sync automatically on every `apply` — no manual CLI step required. (`azuread_application.work_iq` sets `lifecycle.ignore_changes = [web]` so Terraform doesn't fight this out-of-band update.)

Each Factory IQ agent builds this as a standard MCP tool (`ResponseTool.CreateMcpTool`, server label `work-iq`) rather than the A2A tool type — the SDK's `WorkIQPreviewTool`/A2A path never exposed the "send credentials for agent card" option needed for A2A auth, so the MCP endpoint is the supported integration path.

The **Maintenance Agent** and **Plant Manager Agent** use this connection to query and manage Microsoft 365 work items (Planner tasks, open action items, escalation tracking). On first use, expect a one-time `CONSENT_REQUIRED` (`-32006`) response with a consent URL — this is the documented OAuth consent flow, not an error.

## Prerequisites

- Azure subscription with Owner access
- Deployed Factory IQ foundation (Fabric workspace, Eventhouse, Data Agent)
- Fabric Data Agent **published** in Fabric before Foundry agent execution
- .NET 10 SDK
- Azure CLI authenticated

## Configuration

Set the following environment variables (or use `connection.json` output from IaC):

```bash
export AZURE_AI_PROJECT_ENDPOINT="<from connection.json: foundryProjectEndpoint>"
export MODEL_DEPLOYMENT_NAME="gpt-4o"
export AI_SEARCH_ENDPOINT="<from connection.json: aiSearchEndpoint>"
export FOUNDRY_IQ_KNOWLEDGE_BASE_NAME="<from connection.json: foundryIqKnowledgeBaseName>"
export FOUNDRY_IQ_PROJECT_CONNECTION_NAME="<from connection.json: foundryIqProjectConnectionName>"
export FOUNDRY_FABRIC_DATA_AGENT_PROJECT_CONNECTION_NAME="<from connection.json: foundryFabricProjectConnectionName>"
export FOUNDRY_WORK_IQ_PROJECT_CONNECTION_NAME="<from connection.json: foundryWorkIqProjectConnectionName>"  # optional — only for Maintenance and Plant Manager
export STORAGE_ACCOUNT_ENDPOINT="<from connection.json: storageAccountEndpoint>"
export FABRIC_DATA_AGENT_ID="<from connection.json: dataAgentId>"
export FABRIC_WORKSPACE_ID="<from connection.json: workspaceId>"
```

> Fabric OAuth/admin consent remains a manual governance step: tenant admin grants consent once, then end users complete first-use consent if prompted.

> Work IQ requires the tenant admin to grant admin consent for the `WorkIQAgent.Ask` permission on the dedicated Entra app once (Terraform grants this automatically via `azuread_service_principal_delegated_permission_grant`, but tenant policy may still require an admin to approve it in the portal). End users then complete a one-time OAuth consent (`CONSENT_REQUIRED`/`-32006`) on first call, and must hold a Microsoft 365 Copilot license for Work IQ MCP calls to succeed.

Before running Foundry agents, validate the published Data Agent MCP endpoint can enumerate tools:

```bash
TOKEN=$(az account get-access-token --resource https://api.fabric.microsoft.com --query accessToken -o tsv)
curl -X POST "<fabric_data_agent_mcp_target>" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'
```

If `tools/list` returns 404, the Data Agent is not fully published/ready yet for MCP tool execution.

## Running an Agent

```bash
cd src/foundry-agents/agents/FactoryIQ.Agents.Operations
dotnet run
```

## RBAC Requirements

The IaC automatically provisions these role assignments:

| Principal | Target Resource | Role |
|-----------|----------------|------|
| Foundry resource MI | AI Search | Search Index Data Reader |
| Foundry resource MI | AI Search | Search Service Contributor |
| Foundry resource MI | Storage Account | Storage Blob Data Reader |
| Foundry resource MI | Azure OpenAI | Cognitive Services OpenAI User |
| Foundry project MI | AI Search | Search Index Data Reader (Foundry IQ MCP retrieval) |
| AI Search MI | Storage Account | Storage Blob Data Reader (indexer) |
| AI Search MI | Foundry resource | Cognitive Services User |
