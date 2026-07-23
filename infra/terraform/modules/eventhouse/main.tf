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

resource "fabric_kql_queryset" "realtime" {
  display_name = var.kql_queryset_name
  description  = "Factory IQ realtime queryset bootstrap for machine performance diagnostics."
  workspace_id = var.workspace_id
  format       = "Default"

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

  definition = {
    "RealTimeDashboard.json" = {
      source = "${path.module}/definitions/realtime_dashboard.json.tmpl"
      tokens = {
        "KQL_QUERY_URI"     = fabric_kql_database.this.properties.query_service_uri
        "KQL_DATABASE_NAME" = var.kql_database_name
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
