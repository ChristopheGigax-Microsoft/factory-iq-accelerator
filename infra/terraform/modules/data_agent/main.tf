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

  definition = merge(
    {
      "Files/Config/data_agent.json" = {
        source = "${path.module}/definitions/data_agent.json.tmpl"
      }
      "Files/Config/draft/stage_config.json" = {
        source = "${path.module}/definitions/stage_config.json.tmpl"
      }
      "Files/Config/draft/kusto-${var.kql_database_name}/datasource.json" = {
        source = "${path.module}/definitions/datasource_kusto.json.tmpl"
        tokens = {
          "KQL_DATABASE_ID"   = var.kql_database_id
          "WORKSPACE_ID"      = var.workspace_id
          "KQL_DATABASE_NAME" = var.kql_database_name
        }
      }
    },
    var.ontology_id != "" && var.ontology_name != "" ? {
      "Files/Config/draft/ontology-${var.ontology_name}/datasource.json" = {
        source = "${path.module}/definitions/datasource_ontology.json.tmpl"
        tokens = {
          "ONTOLOGY_ID"   = var.ontology_id
          "WORKSPACE_ID"  = var.workspace_id
          "ONTOLOGY_NAME" = var.ontology_name
        }
      }
    } : {}
  )
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

variable "ontology_id" {
  type        = string
  description = "Fabric Ontology item ID to attach as a Data Agent source"
  default     = ""
}

variable "ontology_name" {
  type        = string
  description = "Fabric Ontology display name"
  default     = ""
}

output "data_agent_id" {
  value = fabric_data_agent.this.id
}
