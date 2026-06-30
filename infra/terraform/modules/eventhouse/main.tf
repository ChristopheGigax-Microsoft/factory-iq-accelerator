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

variable "name" {
  type = string
}

variable "workspace_id" {
  type = string
}

variable "kql_database_name" {
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
