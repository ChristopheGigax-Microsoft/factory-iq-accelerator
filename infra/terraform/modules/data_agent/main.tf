terraform {
  required_providers {
    fabric = {
      source = "microsoft/fabric"
    }
  }
}

resource "fabric_data_agent" "this" {
  display_name = var.name
  description  = var.description
  workspace_id = var.workspace_id
  format       = "Default"

  definition = {
    "Files/Config/data_agent.json" = {
      source = "${path.module}/definitions/data_agent.json.tmpl"
    }
    "Files/Config/draft/stage_config.json" = {
      source = "${path.module}/definitions/stage_config.json.tmpl"
    }
    "Files/Config/draft/kusto-${var.kql_database_name}/datasource.json" = {
      source = "${path.module}/definitions/datasource.json.tmpl"
      tokens = {
        "KQL_DATABASE_ID"   = var.kql_database_id
        "WORKSPACE_ID"      = var.workspace_id
        "KQL_DATABASE_NAME" = var.kql_database_name
      }
    }
  }
}

variable "name" {
  type        = string
  description = "Display name of the Data Agent"
}

variable "description" {
  type        = string
  description = "Description of the Data Agent"
  default     = ""
}

variable "workspace_id" {
  type        = string
  description = "Fabric workspace ID"
}

variable "kql_database_id" {
  type        = string
  description = "KQL Database artifact ID (Eventhouse)"
}

variable "kql_database_name" {
  type        = string
  description = "KQL Database display name"
}

output "data_agent_id" {
  value = fabric_data_agent.this.id
}
