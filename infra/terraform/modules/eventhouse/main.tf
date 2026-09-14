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
  bronze_model_script_path = "${path.module}/definitions/bronze_model.kql"
  routing_profile_enabled  = trimspace(var.routing_profile_path) != ""
  routing_profile_dir      = local.routing_profile_enabled ? dirname(var.routing_profile_path) : path.module
  routing_profile_files    = local.routing_profile_enabled ? fileset(local.routing_profile_dir, "**") : toset([])
  routing_profile_hash = local.routing_profile_enabled ? sha256(jsonencode([
    for file in sort(tolist(local.routing_profile_files)) :
    {
      path = file
      hash = filesha256("${local.routing_profile_dir}/${file}")
    }
  ])) : ""
}

resource "terraform_data" "bronze_model" {
  triggers_replace = [
    sha256(jsonencode({
      query_uri   = fabric_kql_database.this.properties.query_service_uri
      database    = var.kql_database_name
      script_hash = filesha256(local.bronze_model_script_path)
    }))
  ]

  input = {
    query_uri   = fabric_kql_database.this.properties.query_service_uri
    database    = var.kql_database_name
    script_path = local.bronze_model_script_path
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

resource "terraform_data" "routing_profile" {
  count = local.routing_profile_enabled ? 1 : 0

  triggers_replace = [
    sha256(jsonencode({
      query_uri    = fabric_kql_database.this.properties.query_service_uri
      database     = var.kql_database_name
      profile_path = var.routing_profile_path
      profile_hash = local.routing_profile_hash
    }))
  ]

  input = {
    query_uri    = fabric_kql_database.this.properties.query_service_uri
    database     = var.kql_database_name
    profile_path = var.routing_profile_path
    deployer     = abspath("${path.root}/../../shared/scripts/deploy-routing-profile.py")
  }

  provisioner "local-exec" {
    interpreter = ["PowerShell", "-NoProfile", "-NonInteractive", "-Command"]
    command     = <<-EOT
      python "$env:ROUTING_DEPLOYER" `
        --profile "$env:ROUTING_PROFILE_PATH" `
        --query-uri "$env:KQL_QUERY_URI" `
        --database "$env:KQL_DATABASE_NAME"
    EOT
    environment = {
      ROUTING_DEPLOYER     = self.input.deployer
      ROUTING_PROFILE_PATH = self.input.profile_path
      KQL_QUERY_URI        = self.input.query_uri
      KQL_DATABASE_NAME    = self.input.database
    }
  }

  depends_on = [terraform_data.bronze_model]
}

resource "fabric_kql_queryset" "realtime" {
  display_name = var.kql_queryset_name
  description  = "Factory IQ realtime queryset for Bronze ingestion diagnostics."
  workspace_id = var.workspace_id
  format       = "Default"
  depends_on   = [terraform_data.bronze_model, terraform_data.routing_profile]

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
  description  = "Factory IQ realtime dashboard for Bronze ingestion verification."
  workspace_id = var.workspace_id
  format       = "Default"
  depends_on   = [terraform_data.bronze_model, terraform_data.routing_profile]

  definition = {
    "RealTimeDashboard.json" = {
      source = "${path.module}/definitions/realtime_dashboard.json.tmpl"
      tokens = {
        "KQL_QUERY_URI"    = fabric_kql_database.this.properties.query_service_uri
        "KQL_DATABASE_ID"  = fabric_kql_database.this.id
        "KQL_WORKSPACE_ID" = var.workspace_id
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

variable "routing_profile_path" {
  type        = string
  description = "Optional absolute path to a routes.json profile."
  default     = ""
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
