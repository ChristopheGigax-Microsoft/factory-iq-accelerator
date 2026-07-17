param foundryName string
param projectName string
param location string
param aiSearchName string
param aiSearchId string
param knowledgeBaseName string
param fabricWorkspaceId string = ''
param fabricDataAgentId string = ''
param fabricDataAgentMcpTarget string = ''
param workIqConnectionTarget string = ''
param plantCode string
param modelDeploymentName string = 'gpt-4o'
var resolvedFabricDataAgentMcpTarget = !empty(fabricDataAgentMcpTarget)
  ? fabricDataAgentMcpTarget
  : 'https://api.fabric.microsoft.com/v1/mcp/workspaces/${fabricWorkspaceId}/dataagents/${fabricDataAgentId}/agent'

// ---------------------------------------------------------------------------
// AI Foundry resource (CognitiveServices/accounts with project management)
// This replaces the deprecated Hub+Project model (MachineLearningServices)
// ---------------------------------------------------------------------------
resource foundry 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: foundryName
  location: location
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    customSubDomainName: foundryName
    allowProjectManagement: true
  }
}

// ---------------------------------------------------------------------------
// Model deployment (GPT-4o) — deployed within the Foundry resource
// ---------------------------------------------------------------------------
resource modelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: foundry
  name: modelDeploymentName
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-11-20'
    }
  }
  sku: {
    name: 'GlobalStandard'
    capacity: 30
  }
}

// ---------------------------------------------------------------------------
// Foundry Project — child of the Foundry resource (not a Hub)
// ---------------------------------------------------------------------------
resource project 'Microsoft.CognitiveServices/accounts/projects@2025-06-01' = {
  parent: foundry
  name: projectName
  location: location
  sku: {
    name: 'S0'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    displayName: 'Factory IQ Agents'
    description: 'Manufacturing agents for plant ${plantCode}'
  }
}

// ---------------------------------------------------------------------------
// Connections (AI Search) — on the Foundry resource, not on a Hub
// ---------------------------------------------------------------------------
resource searchConnection 'Microsoft.CognitiveServices/accounts/connections@2025-04-01-preview' = {
  parent: foundry
  name: 'ai-search-connection'
  properties: {
    category: 'CognitiveSearch'
    authType: 'AAD'
    isSharedToAll: true
    target: 'https://${aiSearchName}.search.windows.net'
    metadata: {
      ApiType: 'Azure'
      ResourceId: aiSearchId
    }
  }
}

// ---------------------------------------------------------------------------
// Foundry IQ project connection (MCP) — binds the Search knowledge base to the
// Foundry project so agents can use knowledge_base_retrieve.
// ---------------------------------------------------------------------------
resource foundryIqKnowledgeBaseConnection 'Microsoft.CognitiveServices/accounts/projects/connections@2025-10-01-preview' = {
  parent: project
  name: 'foundry-iq-kb-connection'
  properties: {
    authType: 'ProjectManagedIdentity'
    category: 'RemoteTool'
    isSharedToAll: true
    target: 'https://${aiSearchName}.search.windows.net/knowledgebases/${knowledgeBaseName}/mcp?api-version=2026-05-01-preview'
    audience: 'https://search.azure.com/'
    metadata: {
      ApiType: 'Azure'
    }
  }
}

// ---------------------------------------------------------------------------
// Fabric IQ (OneLake Catalog) project connection — points to Fabric Data Agent
// MCP endpoint and uses end-user Entra token (OBO).
// ---------------------------------------------------------------------------
resource fabricIqDataAgentConnection 'Microsoft.CognitiveServices/accounts/projects/connections@2025-10-01-preview' = if (!empty(fabricWorkspaceId) && !empty(fabricDataAgentId)) {
  parent: project
  name: 'fabric-iq-data-agent-connection'
  properties: {
    authType: 'UserEntraToken'
    category: 'RemoteTool'
    isSharedToAll: false
    target: resolvedFabricDataAgentMcpTarget
    audience: 'https://api.fabric.microsoft.com'
    metadata: {
      type: 'fabric_iq_preview'
    }
  }
}

// ---------------------------------------------------------------------------
// Work IQ project connection — gives Maintenance and Plant Manager agents
// access to Microsoft 365 work management (Planner, Tasks, work orders).
// Only provisioned when workIqConnectionTarget is set.
// ---------------------------------------------------------------------------
resource workIqConnection 'Microsoft.CognitiveServices/accounts/projects/connections@2025-10-01-preview' = if (!empty(workIqConnectionTarget)) {
  parent: project
  name: 'work-iq-connection'
  properties: {
    authType: 'UserEntraToken'
    category: 'RemoteTool'
    isSharedToAll: false
    target: workIqConnectionTarget
    audience: 'https://work.microsoft.com'
    metadata: {
      type: 'work_iq_preview'
    }
  }
}

output foundryId string = foundry.id
output foundryEndpoint string = foundry.properties.endpoint
output foundryPrincipalId string = foundry.identity.principalId
output projectId string = project.id
output projectPrincipalId string = project.identity.principalId
output modelDeployment string = modelDeployment.name
output foundryIqProjectConnectionName string = foundryIqKnowledgeBaseConnection.name
output foundryFabricProjectConnectionName string = !empty(fabricWorkspaceId) && !empty(fabricDataAgentId)
  ? fabricIqDataAgentConnection.name
  : ''
output foundryWorkIqProjectConnectionName string = !empty(workIqConnectionTarget) ? workIqConnection.name : ''
