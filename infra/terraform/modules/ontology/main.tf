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
    "EntityTypes/7110000000000000002/definition.json" = {
      source = "${path.module}/definitions/entity_work_response.json.tmpl"
    }
    "EntityTypes/7110000000000000003/definition.json" = {
      source = "${path.module}/definitions/entity_quality_test.json.tmpl"
    }
    "EntityTypes/7110000000000000010/definition.json" = {
      source = "${path.module}/definitions/entity_enterprise.json.tmpl"
    }
    "EntityTypes/7110000000000000011/definition.json" = {
      source = "${path.module}/definitions/entity_site.json.tmpl"
    }
    "EntityTypes/7110000000000000012/definition.json" = {
      source = "${path.module}/definitions/entity_area.json.tmpl"
    }
    "EntityTypes/7110000000000000013/definition.json" = {
      source = "${path.module}/definitions/entity_work_center.json.tmpl"
    }
    "EntityTypes/7110000000000000014/definition.json" = {
      source = "${path.module}/definitions/entity_work_unit.json.tmpl"
    }
    "RelationshipTypes/8110000000000000001/definition.json" = {
      source = "${path.module}/definitions/rel_work_response_fulfills_request.json.tmpl"
    }
    "RelationshipTypes/8110000000000000002/definition.json" = {
      source = "${path.module}/definitions/rel_quality_test_validates_response.json.tmpl"
    }
    "RelationshipTypes/8110000000000000010/definition.json" = {
      source = "${path.module}/definitions/rel_enterprise_contains_site.json.tmpl"
    }
    "RelationshipTypes/8110000000000000011/definition.json" = {
      source = "${path.module}/definitions/rel_site_contains_area.json.tmpl"
    }
    "RelationshipTypes/8110000000000000012/definition.json" = {
      source = "${path.module}/definitions/rel_area_contains_work_center.json.tmpl"
    }
    "RelationshipTypes/8110000000000000013/definition.json" = {
      source = "${path.module}/definitions/rel_work_center_contains_work_unit.json.tmpl"
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
