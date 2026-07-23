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
    "EntityTypes/7110000000000000001/definition.json" = {
      source = "${path.module}/definitions/entity_work_request.json.tmpl"
    }
    "EntityTypes/7110000000000000001/DataBindings/11111111-1111-1111-1111-111111111111.json" = {
      source = "${path.module}/definitions/binding_work_request.json.tmpl"
      tokens = {
        "WORKSPACE_ID"      = var.workspace_id
        "EVENTHOUSE_ID"     = var.eventhouse_id
        "KQL_QUERY_URI"     = var.kql_query_uri
        "KQL_DATABASE_NAME" = var.kql_database_name
      }
    }
    "EntityTypes/7110000000000000002/definition.json" = {
      source = "${path.module}/definitions/entity_work_response.json.tmpl"
    }
    "EntityTypes/7110000000000000002/DataBindings/22222222-2222-2222-2222-222222222222.json" = {
      source = "${path.module}/definitions/binding_work_response.json.tmpl"
      tokens = {
        "WORKSPACE_ID"      = var.workspace_id
        "EVENTHOUSE_ID"     = var.eventhouse_id
        "KQL_QUERY_URI"     = var.kql_query_uri
        "KQL_DATABASE_NAME" = var.kql_database_name
      }
    }
    "EntityTypes/7110000000000000003/definition.json" = {
      source = "${path.module}/definitions/entity_quality_test.json.tmpl"
    }
    "EntityTypes/7110000000000000003/DataBindings/33333333-3333-3333-3333-333333333333.json" = {
      source = "${path.module}/definitions/binding_quality_test.json.tmpl"
      tokens = {
        "WORKSPACE_ID"      = var.workspace_id
        "EVENTHOUSE_ID"     = var.eventhouse_id
        "KQL_QUERY_URI"     = var.kql_query_uri
        "KQL_DATABASE_NAME" = var.kql_database_name
      }
    }
    "RelationshipTypes/8110000000000000001/definition.json" = {
      source = "${path.module}/definitions/rel_work_response_fulfills_request.json.tmpl"
    }
    "RelationshipTypes/8110000000000000002/definition.json" = {
      source = "${path.module}/definitions/rel_quality_test_validates_response.json.tmpl"
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
