terraform {
  required_providers {
    fabric = {
      source = "microsoft/fabric"
    }
  }
}

resource "fabric_eventhouse" "this" {
  display_name = var.name
  workspace_id = var.workspace_id
}

resource "fabric_kql_database" "this" {
  display_name = var.kql_database_name
  workspace_id = var.workspace_id

  configuration = {
    database_type = "ReadWrite"
    eventhouse_id = fabric_eventhouse.this.id
  }
}

locals {
  silver_model_script_path = "${path.module}/definitions/silver_model.kql"
}

resource "terraform_data" "silver_model" {
  triggers_replace = [
    sha256(jsonencode({
      query_uri   = fabric_kql_database.this.properties.query_service_uri
      database    = var.kql_database_name
      script_hash = filesha256(local.silver_model_script_path)
    }))
  ]

  input = {
    query_uri   = fabric_kql_database.this.properties.query_service_uri
    database    = var.kql_database_name
    script_path = local.silver_model_script_path
  }

  provisioner "local-exec" {
    interpreter = ["PowerShell", "-NoProfile", "-NonInteractive", "-Command"]
    environment = {
      KQL_QUERY_URI      = self.input.query_uri
      KQL_DATABASE_NAME  = self.input.database
      KQL_MODEL_KQL_PATH = self.input.script_path
    }
    command = <<-EOT
      $token = az account get-access-token --resource https://kusto.kusto.windows.net --query accessToken -o tsv
      if ([string]::IsNullOrWhiteSpace($token)) {
        throw "Unable to acquire Azure Data Explorer token."
      }

      $raw = Get-Content -Path $env:KQL_MODEL_KQL_PATH -Raw -Encoding UTF8
      $commands = New-Object System.Collections.Generic.List[string]
      $current = New-Object System.Text.StringBuilder

      foreach ($line in ($raw -split "`r?`n")) {
        $trimmed = $line.Trim()
        if ($trimmed.StartsWith("//")) {
          continue
        }
        if ($trimmed.StartsWith(".") -and $current.Length -gt 0) {
          $commands.Add($current.ToString().Trim())
          [void]$current.Clear()
        }
        if ($trimmed -eq "" -and $current.Length -eq 0) {
          continue
        }
        [void]$current.AppendLine($line)
      }

      if ($current.Length -gt 0) {
        $commands.Add($current.ToString().Trim())
      }

      $headers = @{
        Authorization = "Bearer $token"
        "Content-Type" = "application/json"
        Accept = "application/json"
      }

      foreach ($commandText in $commands) {
        $body = @{
          db  = $env:KQL_DATABASE_NAME
          csl = $commandText
        } | ConvertTo-Json -Compress

        Invoke-RestMethod -Method Post -Uri "$($env:KQL_QUERY_URI)/v1/rest/mgmt" -Headers $headers -Body $body | Out-Null
      }
    EOT
  }

  depends_on = [fabric_kql_database.this]
}

resource "fabric_kql_queryset" "realtime" {
  display_name = var.kql_queryset_name
  description  = "Factory IQ realtime queryset bootstrap for machine performance diagnostics."
  workspace_id = var.workspace_id
  format       = "Default"
  depends_on   = [terraform_data.silver_model]

  definition = {
    "RealTimeQueryset.json" = {
      source = "${path.module}/definitions/realtime_queryset.json.tmpl"
      tokens = {
        "KQL_QUERY_URI"     = fabric_kql_database.this.properties.query_service_uri
        "KQL_DATABASE_NAME" = var.kql_database_name
      }
    }
  }
}

resource "fabric_kql_dashboard" "realtime" {
  display_name = var.kql_dashboard_name
  description  = "Factory IQ realtime dashboard bootstrap for machine performance verification."
  workspace_id = var.workspace_id
  format       = "Default"
  depends_on   = [terraform_data.silver_model]

  definition = {
    "RealTimeDashboard.json" = {
      source = "${path.module}/definitions/realtime_dashboard.json.tmpl"
      tokens = {
        "KQL_QUERY_URI"     = fabric_kql_database.this.properties.query_service_uri
        "KQL_DATABASE_ID"   = fabric_kql_database.this.id
        "KQL_WORKSPACE_ID"  = var.workspace_id
      }
    }
  }
}

variable "name" {
  type = string
}

variable "workspace_id" {
  type = string
}

variable "kql_database_name" {
  type = string
}

variable "kql_queryset_name" {
  type = string
}

variable "kql_dashboard_name" {
  type = string
}

output "eventhouse_id" {
  value = fabric_eventhouse.this.id
}

output "kql_database_id" {
  value = fabric_kql_database.this.id
}

output "kql_database_name" {
  value = var.kql_database_name
}

output "kql_query_uri" {
  value = fabric_kql_database.this.properties.query_service_uri
}
