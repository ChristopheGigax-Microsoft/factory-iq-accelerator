param(
  [Parameter(Mandatory = $true)][string]$WorkspaceName,
  [Parameter(Mandatory = $true)][string]$EventhouseName,
  [Parameter(Mandatory = $true)][string]$KqlDatabaseName,
  [Parameter(Mandatory = $true)][string]$EventstreamName,
  [Parameter(Mandatory = $true)][string]$DataAgentName
)

$ErrorActionPreference = 'Stop'
$fabricBaseUrl = "https://api.fabric.microsoft.com/v1"

# Authenticate
$token = (Get-AzAccessToken -ResourceUrl "https://api.fabric.microsoft.com").Token
$headers = @{ Authorization = "Bearer $token"; "Content-Type" = "application/json" }

# --- Create Workspace ---
Write-Host "Creating workspace: $WorkspaceName"
$workspaceBody = @{ displayName = $WorkspaceName } | ConvertTo-Json
$workspace = Invoke-RestMethod -Uri "$fabricBaseUrl/workspaces" -Method Post -Headers $headers -Body $workspaceBody
$workspaceId = $workspace.id
Write-Host "Workspace created: $workspaceId"

# --- Create Eventhouse ---
Write-Host "Creating eventhouse: $EventhouseName"
$eventhouseBody = @{ displayName = $EventhouseName; type = "Eventhouse" } | ConvertTo-Json
$eventhouse = Invoke-RestMethod -Uri "$fabricBaseUrl/workspaces/$workspaceId/items" -Method Post -Headers $headers -Body $eventhouseBody
$eventhouseId = $eventhouse.id
Write-Host "Eventhouse created: $eventhouseId"

# --- Create KQL Database ---
Write-Host "Creating KQL database: $KqlDatabaseName"
$kqlDbBody = @{
  displayName    = $KqlDatabaseName
  type           = "KQLDatabase"
  creationPayload = @{
    databaseType  = "ReadWrite"
    parentEventhouseItemId = $eventhouseId
  }
} | ConvertTo-Json -Depth 3
$kqlDb = Invoke-RestMethod -Uri "$fabricBaseUrl/workspaces/$workspaceId/items" -Method Post -Headers $headers -Body $kqlDbBody
$kqlDbId = $kqlDb.id
Write-Host "KQL Database created: $kqlDbId"

# --- Create Eventstream ---
Write-Host "Creating eventstream: $EventstreamName"
$eventstreamBody = @{ displayName = $EventstreamName; type = "Eventstream" } | ConvertTo-Json
Invoke-RestMethod -Uri "$fabricBaseUrl/workspaces/$workspaceId/items" -Method Post -Headers $headers -Body $eventstreamBody
Write-Host "Eventstream created"

# --- Create Data Agent with Eventhouse data source ---
Write-Host "Creating data agent: $DataAgentName"

$dataAgentConfig = @{ '$schema' = "https://developer.microsoft.com/json-schemas/fabric/item/dataAgent/definition/dataAgent/2.1.0/schema.json" } | ConvertTo-Json
$stageConfig = @{
  '$schema'      = "https://developer.microsoft.com/json-schemas/fabric/item/dataAgent/definition/stageConfiguration/1.0.0/schema.json"
  aiInstructions = "You are a Factory IQ data assistant. Answer questions about manufacturing data using KQL queries against the Eventhouse database."
} | ConvertTo-Json
$datasourceConfig = @{
  '$schema'        = "https://developer.microsoft.com/json-schemas/fabric/item/dataAgent/definition/dataSource/1.0.0/schema.json"
  artifactId       = $kqlDbId
  workspaceId      = $workspaceId
  displayName      = $KqlDatabaseName
  type             = "kusto"
  userDescription  = "Factory IQ Eventhouse KQL Database"
} | ConvertTo-Json

$dataAgentBody = @{
  displayName = $DataAgentName
  type        = "DataAgent"
  definition  = @{
    parts = @(
      @{
        path        = "Files/Config/data_agent.json"
        payload     = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($dataAgentConfig))
        payloadType = "InlineBase64"
      },
      @{
        path        = "Files/Config/draft/stage_config.json"
        payload     = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($stageConfig))
        payloadType = "InlineBase64"
      },
      @{
        path        = "Files/Config/draft/kusto-$KqlDatabaseName/datasource.json"
        payload     = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($datasourceConfig))
        payloadType = "InlineBase64"
      }
    )
  }
} | ConvertTo-Json -Depth 5

Invoke-RestMethod -Uri "$fabricBaseUrl/workspaces/$workspaceId/items" -Method Post -Headers $headers -Body $dataAgentBody
Write-Host "Data Agent created with Eventhouse data source linked"
