param plantCode string
param environment string
param region string
param capacitySku string = 'F2'

var baseName = 'fiq-${plantCode}-${environment}'
var workspaceName = '${baseName}-ws'
var eventhouseName = '${baseName}-eh'
var kqlDatabaseName = '${baseName}-kql'
var eventstreamName = '${baseName}-es'

module capacity './modules/capacity.bicep' = {
  name: 'capacity'
  params: {
    capacityName: '${baseName}-cap'
    location: region
    capacitySku: capacitySku
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
    arguments: '-WorkspaceName ${workspaceName} -EventhouseName ${eventhouseName} -KqlDatabaseName ${kqlDatabaseName} -EventstreamName ${eventstreamName}'
  }
}

output connectionContract object = {
  tenantId: subscription().tenantId
  subscriptionId: subscription().subscriptionId
  resourceGroup: resourceGroup().name
  region: region
  workspaceId: workspaceName
  eventhouseId: eventhouseName
  kqlDatabase: kqlDatabaseName
  generatedAt: utcNow()
  schemaVersion: '1.0'
}
