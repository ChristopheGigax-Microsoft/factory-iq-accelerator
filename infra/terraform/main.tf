terraform {
  required_version = ">= 1.6.0"

  required_providers {
    fabric = {
      source  = "microsoft/fabric"
      version = ">= 1.0.0"
    }
  }
}

locals {
  base_name = "fiq-${var.plant_code}-${var.environment}"
}

module "workspace" {
  source      = "./modules/workspace"
  name        = "${local.base_name}-ws"
}

module "eventhouse" {
  source            = "./modules/eventhouse"
  name              = "${local.base_name}-eh"
  workspace_id      = module.workspace.workspace_id
  kql_database_name = "${local.base_name}-kql"
}

module "eventstream" {
  source          = "./modules/eventstream"
  name            = "${local.base_name}-es"
  workspace_id    = module.workspace.workspace_id
  definition_path = "../../shared/eventstream/definition/eventstream.json"
}
