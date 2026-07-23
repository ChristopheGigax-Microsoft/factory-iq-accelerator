param(
  [Parameter(Mandatory = $true)][string]$WorkspaceName,
  [Parameter(Mandatory = $true)][string]$EventhouseName,
  [Parameter(Mandatory = $true)][string]$KqlDatabaseName,
  [Parameter(Mandatory = $true)][string]$EventstreamName,
  [Parameter(Mandatory = $true)][string]$DataAgentName,
  [Parameter(Mandatory = $true)][string]$OntologyName,
  [Parameter(Mandatory = $true)][string]$QuerysetName,
  [Parameter(Mandatory = $true)][string]$DashboardName
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

# --- Resolve KQL Query Service URI ---
Write-Host "Resolving KQL query service URI"
$kqlDbDetails = Invoke-RestMethod -Uri "$fabricBaseUrl/workspaces/$workspaceId/kqlDatabases/$kqlDbId" -Method Get -Headers $headers
$kqlQueryUri = $kqlDbDetails.properties.queryServiceUri
if ([string]::IsNullOrWhiteSpace($kqlQueryUri)) {
  throw "KQL queryServiceUri is missing for database '$KqlDatabaseName' ($kqlDbId)."
}
Write-Host "KQL query service URI resolved"

# --- Create Eventstream ---
Write-Host "Creating eventstream: $EventstreamName"
$eventstreamBody = @{ displayName = $EventstreamName; type = "Eventstream" } | ConvertTo-Json
Invoke-RestMethod -Uri "$fabricBaseUrl/workspaces/$workspaceId/items" -Method Post -Headers $headers -Body $eventstreamBody
Write-Host "Eventstream created"

# --- Create Real-Time Queryset ---
Write-Host "Creating realtime queryset: $QuerysetName"
$querysetDefinition = @{
  queryset = @{
    version = "1.0.0"
    dataSources = @(
      @{
        id = "f2aebd2d-7b4f-4c4c-b13a-4dbe9f5ec0f6"
        clusterUri = $kqlQueryUri
        type = "AzureDataExplorer"
        databaseName = $KqlDatabaseName
      }
    )
    tabs = @(
      @{
        id = "87af4acf-80ec-4a8e-a3b5-5fdf4900ec6e"
        content = "EquipmentActual`n| where ingestion_time() > ago(60m)`n| extend WorkUnit=tostring(column_ifexists(""WorkUnitId"", ""Unknown""))`n| summarize StateEvents=count() by WorkUnit`n| order by StateEvents desc"
        title = "Machine state events (60m)"
        dataSourceId = "f2aebd2d-7b4f-4c4c-b13a-4dbe9f5ec0f6"
      }
      @{
        id = "9daf8d7c-c47d-4d4b-982a-90f1fb3d02ef"
        content = "EquipmentTelemetry`n| where ingestion_time() > ago(30m)`n| extend WorkUnit=tostring(column_ifexists(""WorkUnitId"", ""Unknown""))`n| summarize Samples=count() by TimeBucket=bin(ingestion_time(), 1m), WorkUnit`n| order by TimeBucket asc"
        title = "Telemetry activity trend (30m)"
        dataSourceId = "f2aebd2d-7b4f-4c4c-b13a-4dbe9f5ec0f6"
      }
      @{
        id = "42cec22a-7fdb-42ba-a575-f6119770f85d"
        content = "WorkResponse`n| where ingestion_time() > ago(60m)`n| extend WorkUnit=tostring(column_ifexists(""WorkUnitId"", ""Unknown""))`n| extend GoodQty=toint(column_ifexists(""GoodQuantity"", 0)), RejectQty=toint(column_ifexists(""RejectQuantity"", 0))`n| summarize Good=sum(GoodQty), Reject=sum(RejectQty), Responses=count() by WorkUnit`n| order by Good desc"
        title = "Throughput and rejects (60m)"
        dataSourceId = "f2aebd2d-7b4f-4c4c-b13a-4dbe9f5ec0f6"
      }
      @{
        id = "725a18b8-7402-4acf-8bb8-c9060af3927d"
        content = "QualityTestResult`n| where ingestion_time() > ago(60m)`n| extend Result=tostring(column_ifexists(""Result"", ""unknown""))`n| summarize Samples=count() by Result`n| order by Samples desc"
        title = "Quality outcomes (60m)"
        dataSourceId = "f2aebd2d-7b4f-4c4c-b13a-4dbe9f5ec0f6"
      }
      @{
        id = "ea345f66-3bb5-4568-b4ab-c69e41e3c510"
        content = "WorkRequest`n| where ingestion_time() > ago(24h)`n| extend Priority=tostring(column_ifexists(""Priority"", ""unknown""))`n| summarize Requests=count() by Priority`n| order by Requests desc"
        title = "Workload mix by priority (24h)"
        dataSourceId = "f2aebd2d-7b4f-4c4c-b13a-4dbe9f5ec0f6"
      }
    )
  }
} | ConvertTo-Json -Depth 8 -Compress

$querysetBody = @{
  displayName = $QuerysetName
  type        = "KQLQueryset"
  definition  = @{
    parts = @(
      @{
        path        = "RealTimeQueryset.json"
        payload     = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($querysetDefinition))
        payloadType = "InlineBase64"
      }
    )
  }
} | ConvertTo-Json -Depth 8

Invoke-RestMethod -Uri "$fabricBaseUrl/workspaces/$workspaceId/items" -Method Post -Headers $headers -Body $querysetBody
Write-Host "Realtime queryset created"

# --- Create Real-Time Dashboard bootstrap ---
Write-Host "Creating realtime dashboard: $DashboardName"
$dashboardDefinition = @{
  autoRefresh = @{
    enabled = $true
  }
  baseQueries = @()
  tiles = @()
  dataSources = @(
    @{
      id = "9a9f91ee-3d69-4f4a-8eb8-fd2d0920a622"
      name = "Factory IQ Eventhouse"
      clusterUri = $kqlQueryUri
      database = $KqlDatabaseName
      kind = "KQLDatabase"
      scopeId = "9a9f91ee-3d69-4f4a-8eb8-fd2d0920a622"
    }
  )
  pages = @(
    @{
      name = "Machine performance overview"
      id = "be683a2b-4119-483f-a477-5e7ac959b8c8"
    }
  )
  parameters = @(
    @{
      kind = "duration"
      id = "b6d3f809-8fcb-4a9e-8fa8-0c6dfaf25f6d"
      displayName = "Time range"
      description = "Time range shared by dashboard visuals."
      beginVariableName = "_startTime"
      endVariableName = "_endTime"
      defaultValue = @{
        kind = "dynamic"
        count = 1
        unit = "hours"
      }
      showOnPages = @{
        kind = "all"
      }
    }
  )
  queries = @()
  schema_version = "52"
  title = "Factory IQ Real-Time Machine Performance"
} | ConvertTo-Json -Depth 8 -Compress

$dashboardBody = @{
  displayName = $DashboardName
  type        = "KQLDashboard"
  definition  = @{
    parts = @(
      @{
        path        = "RealTimeDashboard.json"
        payload     = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($dashboardDefinition))
        payloadType = "InlineBase64"
      }
    )
  }
} | ConvertTo-Json -Depth 8

Invoke-RestMethod -Uri "$fabricBaseUrl/workspaces/$workspaceId/items" -Method Post -Headers $headers -Body $dashboardBody
Write-Host "Realtime dashboard bootstrap created"

# --- Create Ontology ---
Write-Host "Creating ontology: $OntologyName"
$ontologyPlatform = @{
  metadata = @{
    type = "Ontology"
    displayName = $OntologyName
  }
} | ConvertTo-Json -Depth 3

$ontologyDefinition = @(
  @{
    path        = ".platform"
    payload     = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($ontologyPlatform))
    payloadType = "InlineBase64"
  },
  @{
    path        = "definition.json"
    payload     = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("{}"))
    payloadType = "InlineBase64"
  }
)

$ontologyBody = @{
  displayName = $OntologyName
  type        = "Ontology"
  definition  = @{
    parts = $ontologyDefinition
  }
} | ConvertTo-Json -Depth 6

$ontology = Invoke-RestMethod -Uri "$fabricBaseUrl/workspaces/$workspaceId/items" -Method Post -Headers $headers -Body $ontologyBody
$ontologyId = $ontology.id
Write-Host "Ontology created: $ontologyId"

# --- Create Data Agent with Eventhouse data source ---
Write-Host "Creating data agent: $DataAgentName"

$dataAgentConfig = @{ '$schema' = "https://developer.microsoft.com/json-schemas/fabric/item/dataAgent/definition/dataAgent/2.1.0/schema.json" } | ConvertTo-Json

$aiInstructions = @"
You are Factory IQ, an expert manufacturing data assistant for ISA-95 (IEC 62264) compliant factories.

Data architecture:
- KQL Eventhouse: real-time operations data (equipment states, work orders, material tracking, quality results, telemetry)
- SQL Database (separate): ISA-95 hierarchy dimensions (Enterprise > Site > Area > WorkCenter > WorkUnit)
- Ontology (optional, if configured in Data Agent): business semantic layer for ISA-95 concepts and KPI definitions
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
- If ontology source is available, use it for business-term interpretation and KPI semantics, then ground diagnostics in KQL
- For ontology aggregation quality, include instruction: Support group by in GQL
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

$datasourceOntology = @{
  '$schema'       = "https://developer.microsoft.com/json-schemas/fabric/item/dataAgent/definition/dataSource/1.0.0/schema.json"
  artifactId      = $ontologyId
  workspaceId     = $workspaceId
  displayName     = $OntologyName
  type            = "ontology"
  userDescription = "Factory IQ ISA-95 ontology source for business semantics, KPI definitions, and relationship-aware reasoning."
} | ConvertTo-Json -Depth 3

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
  },
  @{
    path        = "Files/Config/draft/ontology-$OntologyName/datasource.json"
    payload     = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($datasourceOntology))
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
Write-Host "Data Agent created with Eventhouse + Ontology sources, table selection, and AI instructions"
