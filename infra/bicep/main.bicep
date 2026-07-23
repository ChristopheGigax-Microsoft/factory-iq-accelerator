param plantCode string
param environment string
param region string
param capacitySku string = 'F2'
@description('Fabric workspace GUID for Fabric IQ Data Agent connection (optional for Bicep flow).')
param fabricWorkspaceId string = ''
@description('Fabric Data Agent GUID for Fabric IQ Data Agent connection (optional for Bicep flow).')
param fabricDataAgentId string = ''
@description('Optional full Fabric Data Agent MCP endpoint URL for Fabric IQ connection. If empty, Bicep uses the global api.fabric.microsoft.com endpoint.')
param fabricDataAgentMcpTarget string = ''
@description('Work IQ connection target URL for Maintenance and Plant Manager agents. Obtain from your M365 Work IQ service endpoint. Leave empty to skip.')
param workIqConnectionTarget string = ''

var baseName = 'fiq-${plantCode}-${environment}'
var workspaceName = '${baseName}-ws'
var eventhouseName = '${baseName}-eh'
var kqlDatabaseName = '${baseName}-kql'
var eventstreamName = '${baseName}-es'
var dataAgentName = '${baseName}-agent'
var ontologyName = replace('${baseName}_ontology', '-', '_')
var querysetName = '${baseName}-rtqs'
var dashboardName = '${baseName}-rtd'
var aiSearchName = '${baseName}-search'
var storageAccountName = replace('fiq${plantCode}${environment}sa', '-', '')
var aiProjectName = '${baseName}-ai-project'
var aiFoundryName = '${baseName}-ai-foundry'
var foundryIqKnowledgeBaseName = '${baseName}-kb'

module capacity './modules/capacity.bicep' = {
  name: 'capacity'
  params: {
    capacityName: '${baseName}-cap'
    location: region
    capacitySku: capacitySku
  }
}

module storageAccount './modules/storage-account.bicep' = {
  name: 'storageAccount'
  params: {
    name: storageAccountName
    location: region
  }
}

module aiSearch './modules/ai-search.bicep' = {
  name: 'aiSearch'
  params: {
    name: aiSearchName
    location: region
  }
}

module aiFoundry './modules/ai-foundry.bicep' = {
  name: 'aiFoundry'
  params: {
    foundryName: aiFoundryName
    projectName: aiProjectName
    location: region
    aiSearchName: aiSearch.outputs.name
    aiSearchId: aiSearch.outputs.id
    knowledgeBaseName: foundryIqKnowledgeBaseName
    fabricWorkspaceId: fabricWorkspaceId
    fabricDataAgentId: fabricDataAgentId
    fabricDataAgentMcpTarget: fabricDataAgentMcpTarget
    workIqConnectionTarget: workIqConnectionTarget
    plantCode: plantCode
  }
}

module rbac './modules/rbac.bicep' = {
  name: 'rbac'
  params: {
    foundryPrincipalId: aiFoundry.outputs.foundryPrincipalId
    projectPrincipalId: aiFoundry.outputs.projectPrincipalId
    aiSearchId: aiSearch.outputs.id
    aiSearchPrincipalId: aiSearch.outputs.principalId
    storageAccountId: storageAccount.outputs.id
  }
}

resource createItems 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: 'create-fabric-items'
  location: region
  kind: 'AzurePowerShell'
  properties: {
    azPowerShellVersion: '11.4'
    timeout: 'PT30M'
    retentionInterval: 'P1D'
    scriptContent: loadTextContent('./scripts/create-fabric-items.ps1')
    arguments: '-WorkspaceName ${workspaceName} -EventhouseName ${eventhouseName} -KqlDatabaseName ${kqlDatabaseName} -EventstreamName ${eventstreamName} -DataAgentName ${dataAgentName} -OntologyName ${ontologyName} -QuerysetName ${querysetName} -DashboardName ${dashboardName}'
  }
}

param deploymentTimestamp string = utcNow()

output connectionContract object = {
  tenantId: subscription().tenantId
  subscriptionId: subscription().subscriptionId
  resourceGroup: resourceGroup().name
  region: region
  workspaceId: workspaceName
  eventhouseId: eventhouseName
  kqlDatabase: kqlDatabaseName
  dataAgentId: dataAgentName
  fabricOntologyName: ontologyName
  foundryEndpoint: aiFoundry.outputs.foundryEndpoint
  foundryProjectId: aiFoundry.outputs.projectId
  foundryIqProjectConnectionName: aiFoundry.outputs.foundryIqProjectConnectionName
  foundryFabricProjectConnectionName: aiFoundry.outputs.foundryFabricProjectConnectionName
  foundryWorkIqProjectConnectionName: aiFoundry.outputs.foundryWorkIqProjectConnectionName
  aiSearchEndpoint: aiSearch.outputs.endpoint
  foundryIqKnowledgeBaseName: foundryIqKnowledgeBaseName
  modelDeploymentName: aiFoundry.outputs.modelDeployment
  storageAccountEndpoint: storageAccount.outputs.primaryBlobEndpoint
  generatedAt: deploymentTimestamp
  schemaVersion: '3.0'
}
