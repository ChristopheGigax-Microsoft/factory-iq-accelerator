terraform {
  required_providers {
    fabric = {
      source = "microsoft/fabric"
    }
  }
}

resource "fabric_ontology" "this" {
  display_name = var.name
  description  = var.description
  workspace_id = var.workspace_id
  format       = "Default"

  definition = {
    "definition.json" = {
      source = "${path.module}/definitions/definition.json.tmpl"
    }
  }
}

variable "name" {
  type        = string
  description = "Ontology display name"
}

variable "description" {
  type        = string
  description = "Ontology description"
  default     = ""
}

variable "workspace_id" {
  type        = string
  description = "Fabric workspace ID"
}

variable "eventhouse_id" {
  type        = string
  description = "Fabric Eventhouse artifact ID"
}

variable "kql_query_uri" {
  type        = string
  description = "Fabric KQL query service URI"
}

variable "kql_database_name" {
  type        = string
  description = "Fabric KQL database display name"
}

output "ontology_id" {
  value = fabric_ontology.this.id
}

output "ontology_name" {
  value = var.name
}
