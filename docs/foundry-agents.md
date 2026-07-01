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

The Fabric Data Agent is already provisioned in the Fabric workspace (via IaC). Foundry agents call it as a tool to run natural-language queries that are translated into KQL against the Eventhouse. Uses On-Behalf-Of (OBO) authorization.

### 2. Fabric IQ (Semantic Layer)

Fabric IQ is the semantic intelligence layer that provides ontology-grounded access to Fabric data. It ensures agents interpret data consistently using shared business definitions (entities, relationships, metrics). This prevents hallucination and provides reliable, contextual answers.

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

These documents are uploaded to the Storage Account's `knowledge-base` container and indexed by Azure AI Search for RAG-based retrieval.

## Prerequisites

- Azure subscription with Owner access
- Deployed Factory IQ foundation (Fabric workspace, Eventhouse, Data Agent)
- .NET 10 SDK
- Azure CLI authenticated

## Configuration

Set the following environment variables (or use `connection.json` output from IaC):

```bash
export AZURE_AI_PROJECT_ENDPOINT="<from connection.json: foundryProjectEndpoint>"
export MODEL_DEPLOYMENT_NAME="gpt-4o"
export AI_SEARCH_ENDPOINT="<from connection.json: aiSearchEndpoint>"
export STORAGE_ACCOUNT_ENDPOINT="<from connection.json: storageAccountEndpoint>"
export FABRIC_DATA_AGENT_ID="<from connection.json: dataAgentId>"
export FABRIC_WORKSPACE_ID="<from connection.json: workspaceId>"
```

## Running an Agent

```bash
cd src/foundry-agents/agents/FactoryIQ.Agents.Operations
dotnet run
```

## RBAC Requirements

The IaC automatically provisions these role assignments:

| Principal | Target Resource | Role |
|-----------|----------------|------|
| Foundry Project MI | AI Search | Search Index Data Reader |
| Foundry Project MI | AI Search | Search Service Contributor |
| Foundry Project MI | Storage Account | Storage Blob Data Reader |
| Foundry Project MI | Azure OpenAI | Cognitive Services OpenAI User |
| AI Search MI | Storage Account | Storage Blob Data Reader (indexer) |
| Foundry Hub MI | AI Search | Search Index Data Reader |
| Foundry Hub MI | Storage Account | Storage Blob Data Contributor |
