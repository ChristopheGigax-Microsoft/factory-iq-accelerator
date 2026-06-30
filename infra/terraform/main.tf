terraform {
  required_version = ">= 1.6.0"

  required_providers {
    azapi = {
      source  = "Azure/azapi"
      version = ">= 2.0.0"
    }
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">= 4.2.0"
    }
    fabric = {
      source  = "microsoft/fabric"
      version = ">= 1.0.0"
    }
  }
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
  tenant_id       = var.tenant_id
}

provider "azapi" {
  subscription_id = var.subscription_id
  tenant_id       = var.tenant_id
}

locals {
  base_name = "fiq-${var.plant_code}-${var.environment}"
}

resource "azurerm_resource_group" "this" {
  name     = var.resource_group
  location = var.region
}

module "capacity" {
  source            = "./modules/capacity"
  name              = "${replace(local.base_name, "-", "")}cap"
  location          = var.region
  sku               = var.capacity_sku
  resource_group_id = azurerm_resource_group.this.id
  admin_members     = var.capacity_admin_members
}

module "workspace" {
  source      = "./modules/workspace"
  name        = "${local.base_name}-ws"
  capacity_id = module.capacity.capacity_id
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

module "data_agent" {
  source            = "./modules/data_agent"
  name              = "${local.base_name}-agent"
  description       = "Factory IQ Data Agent for plant ${var.plant_code}"
  workspace_id      = module.workspace.workspace_id
  kql_database_id   = module.eventhouse.kql_database_id
  kql_database_name = module.eventhouse.kql_database_name
}
