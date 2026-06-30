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

$aiInstructions = @"
You are Factory IQ, an expert manufacturing data assistant for ISA-95 (IEC 62264) compliant factories.

Data architecture:
- KQL Eventhouse: real-time operations data (equipment states, work orders, material tracking, quality results, telemetry)
- SQL Database (separate): ISA-95 hierarchy dimensions (Enterprise > Site > Area > WorkCenter > WorkUnit)
- You only query the KQL Eventhouse. WorkUnitId/WorkCenterId are foreign keys to the hierarchy.

ISA-95 tables you can query:
- EquipmentActual: equipment state transitions (Running, Idle, Held, Stopped, Aborted, Fault, Maintenance) with StateReason and OperatorId
- WorkRequest: production/work orders from MES/ERP (product, quantity, schedule, priority)
- WorkResponse: actual execution results (quantities produced/rejected, actual times)
- MaterialActual: material lot consumption (Direction='consumed') and production (Direction='produced') per work request
- QualityTestResult: inspection results with measured values vs specification limits (pass/fail)
- EquipmentTelemetry: OPC-UA sensor signals (temperature, pressure, vibration) - cleaned, use for analytics

Guidelines:
- Use KQL for all queries
- Default to last 24 hours unless specified
- For OEE: Availability from EquipmentActual (Running time / total), Performance from EquipmentTelemetry (actual vs nominal rate), Quality from WorkResponse (good / total produced)
- For traceability: follow WorkRequest > WorkResponse > MaterialActual > QualityTestResult chain
- Never query TelemetryLanding (raw ingestion table) - use EquipmentTelemetry instead
- Provide concise, actionable answers with KPIs and trends
"@

$stageConfig = @{
  '$schema'      = "https://developer.microsoft.com/json-schemas/fabric/item/dataAgent/definition/stageConfiguration/1.0.0/schema.json"
  aiInstructions = $aiInstructions
} | ConvertTo-Json

$dataSourceInstructions = @"
This KQL database stores real-time factory operations data following the ISA-95 (IEC 62264) standard. ISA-95 hierarchy dimensions are in a separate SQL database.

ISA-95 Operations tables:
- EquipmentActual: equipment state transitions per WorkUnit following ISA-95/PackML states (Running, Idle, Held, Stopped, Aborted, Fault, Maintenance). Includes StateReason and OperatorId.
- WorkRequest: production/work orders from MES/ERP with product, quantity, schedule, and priority.
- WorkResponse: actual execution results of work requests - quantities produced/rejected, actual start/end times.
- MaterialActual: material lot consumption and production linked to work requests. Direction is 'consumed' or 'produced'.
- QualityTestResult: quality inspection results with measured values vs specification limits, linked to work responses and material lots. Result is 'pass' or 'fail'.

Telemetry tables (OPC-UA / ISA-88 complementary):
- EquipmentTelemetry: cleaned sensor signals (temperature, pressure, vibration) per WorkUnit - use this for analytics.
- TelemetryLanding: raw Eventstream ingestion - do NOT query directly.

Key relationships:
- WorkRequest.RequestId > WorkResponse.RequestId (order > execution)
- WorkResponse.ResponseId > QualityTestResult.ResponseId (execution > quality)
- WorkRequest.RequestId > MaterialActual.RequestId (order > material)
- MaterialActual.LotId > QualityTestResult.LotId (material > quality traceability)
"@

$datasourceKusto = @{
  '$schema'              = "https://developer.microsoft.com/json-schemas/fabric/item/dataAgent/definition/dataSource/1.0.0/schema.json"
  artifactId             = $kqlDbId
  workspaceId            = $workspaceId
  displayName            = $KqlDatabaseName
  type                   = "kusto"
  userDescription        = "Factory IQ Eventhouse - real-time ISA-95 operations data (equipment, production, material, quality)."
  dataSourceInstructions = $dataSourceInstructions
  elements               = @(
    @{ display_name = "EquipmentActual";     type = "kusto.table"; is_selected = $true;  description = "ISA-95 Equipment Actual - state transitions per work unit (Running, Idle, Fault, Maintenance, etc.)" }
    @{ display_name = "WorkRequest";         type = "kusto.table"; is_selected = $true;  description = "ISA-95 Work Request - production/work orders with product, quantity, schedule" }
    @{ display_name = "WorkResponse";        type = "kusto.table"; is_selected = $true;  description = "ISA-95 Work Response - actual execution results with quantities produced/rejected" }
    @{ display_name = "MaterialActual";      type = "kusto.table"; is_selected = $true;  description = "ISA-95 Material Actual - material lot consumption and production per work request" }
    @{ display_name = "QualityTestResult";   type = "kusto.table"; is_selected = $true;  description = "ISA-95 Quality Test Result - inspection results with measured values vs spec limits" }
    @{ display_name = "EquipmentTelemetry";  type = "kusto.table"; is_selected = $true;  description = "OPC-UA sensor telemetry - cleaned signals (temperature, pressure, vibration) per work unit" }
    @{ display_name = "TelemetryLanding";    type = "kusto.table"; is_selected = $false; description = "Raw Eventstream ingestion - do NOT query, use EquipmentTelemetry instead" }
  )
} | ConvertTo-Json -Depth 4

$parts = @(
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
    payload     = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($datasourceKusto))
    payloadType = "InlineBase64"
  }
)

$dataAgentBody = @{
  displayName = $DataAgentName
  type        = "DataAgent"
  definition  = @{
    parts = $parts
  }
} | ConvertTo-Json -Depth 5

Invoke-RestMethod -Uri "$fabricBaseUrl/workspaces/$workspaceId/items" -Method Post -Headers $headers -Body $dataAgentBody
Write-Host "Data Agent created with Eventhouse data source, table selection, and AI instructions"
