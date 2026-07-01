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
  base_name    = "fiq-${var.plant_code}-${var.environment}"
  workspace_id = var.workspace_id
}

resource "azurerm_resource_group" "this" {
  name     = var.resource_group
  location = var.region
}

module "eventhouse" {
  source            = "./modules/eventhouse"
  name              = "${local.base_name}-eh"
  workspace_id      = local.workspace_id
  kql_database_name = "${local.base_name}-kql"
}

module "eventstream" {
  source          = "./modules/eventstream"
  name            = "${local.base_name}-es"
  workspace_id    = local.workspace_id
  definition_path = "../../shared/eventstream/definition/eventstream.json"
}

module "data_agent" {
  source            = "./modules/data_agent"
  name              = "${local.base_name}-agent"
  description       = "Factory IQ Data Agent for plant ${var.plant_code}"
  workspace_id      = local.workspace_id
  kql_database_id   = module.eventhouse.kql_database_id
  kql_database_name = module.eventhouse.kql_database_name
}

module "storage_account" {
  source              = "./modules/storage_account"
  name                = replace("fiq${var.plant_code}${var.environment}sa", "-", "")
  location            = var.region
  resource_group_name = azurerm_resource_group.this.name
}

module "ai_search" {
  source              = "./modules/ai_search"
  name                = "${local.base_name}-search"
  location            = var.region
  resource_group_name = azurerm_resource_group.this.name
}

module "ai_foundry" {
  source              = "./modules/ai_foundry"
  foundry_name        = "${local.base_name}-ai-foundry"
  project_name        = "${local.base_name}-ai-project"
  location            = var.region
  resource_group_name = azurerm_resource_group.this.name
  plant_code          = var.plant_code
  ai_search_name      = module.ai_search.name
  ai_search_id        = module.ai_search.id
}

module "rbac" {
  source                  = "./modules/rbac"
  foundry_principal_id    = module.ai_foundry.foundry_principal_id
  ai_search_id            = module.ai_search.id
  ai_search_principal_id  = module.ai_search.principal_id
  storage_account_id      = module.storage_account.id
}
