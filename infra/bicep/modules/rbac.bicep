param foundryPrincipalId string
param aiSearchId string
param aiSearchPrincipalId string
param storageAccountId string

// Role definition IDs
var searchIndexDataReader = '1407120a-92aa-4202-b7e9-c0e197c71c8f'
var searchServiceContributor = '7ca78c08-252a-4471-8644-bb5ff32d4ba0'
var storageBlobDataReader = '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1'

// Foundry resource MI → AI Search: Search Index Data Reader
resource foundrySearchReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiSearchId, foundryPrincipalId, searchIndexDataReader)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', searchIndexDataReader)
    principalId: foundryPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Foundry resource MI → AI Search: Search Service Contributor
resource foundrySearchContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiSearchId, foundryPrincipalId, searchServiceContributor)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', searchServiceContributor)
    principalId: foundryPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Foundry resource MI → Storage: Storage Blob Data Reader
resource foundryStorageReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountId, foundryPrincipalId, storageBlobDataReader)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataReader)
    principalId: foundryPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// AI Search MI → Storage: Storage Blob Data Reader (indexer access)
resource searchStorageReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountId, aiSearchPrincipalId, storageBlobDataReader)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataReader)
    principalId: aiSearchPrincipalId
    principalType: 'ServicePrincipal'
  }
}
