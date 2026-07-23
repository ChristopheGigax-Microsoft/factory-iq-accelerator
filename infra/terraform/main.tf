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

provider "fabric" {
  preview = true
}

moved {
  from = module.ai_foundry.azapi_resource.search_connection
  to   = azapi_resource.search_connection
}

locals {
  base_name                    = "fiq-${var.plant_code}-${var.environment}"
  workspace_id                 = var.workspace_id
  ontology_name                = replace("${local.base_name}_ontology", "-", "_")
  fabric_data_agent_mcp_target = trimspace(var.fabric_data_agent_mcp_target) != "" ? trimspace(var.fabric_data_agent_mcp_target) : "https://api.fabric.microsoft.com/v1/mcp/workspaces/${local.workspace_id}/dataagents/${var.fabric_data_agent_id}/agent"
  work_iq_connection_target    = trimspace(var.work_iq_connection_target)
}

resource "azurerm_resource_group" "this" {
  name     = var.resource_group
  location = var.region
}

module "eventhouse" {
  source             = "./modules/eventhouse"
  name               = "${local.base_name}-eh"
  workspace_id       = local.workspace_id
  kql_database_name  = "${local.base_name}-kql"
  kql_queryset_name  = "${local.base_name}-rtqs"
  kql_dashboard_name = "${local.base_name}-rtd"
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
  ontology_id       = module.ontology.ontology_id
  ontology_name     = module.ontology.ontology_name
}

module "ontology" {
  source            = "./modules/ontology"
  name              = local.ontology_name
  description       = "Factory IQ ISA-95 operations ontology for plant ${var.plant_code}"
  workspace_id      = local.workspace_id
  eventhouse_id     = module.eventhouse.eventhouse_id
  kql_query_uri     = module.eventhouse.kql_query_uri
  kql_database_name = module.eventhouse.kql_database_name
}

module "storage_account" {
  source              = "./modules/storage_account"
  name                = replace("fiq${var.plant_code}${var.environment}sa", "-", "")
  location            = var.region
  resource_group_name = azurerm_resource_group.this.name
}

module "ai_search" {
  source                    = "./modules/ai_search"
  name                      = "${local.base_name}-search"
  location                  = var.region
  resource_group_name       = azurerm_resource_group.this.name
  knowledge_source_name     = "${local.base_name}-blob-ks"
  knowledge_base_name       = "${local.base_name}-kb"
  storage_connection_string = module.storage_account.primary_connection_string
  foundry_endpoint          = module.ai_foundry.foundry_endpoint
  embedding_deployment_name = module.ai_foundry.embedding_deployment_name
  model_deployment_name     = module.ai_foundry.model_deployment_name
}

module "ai_foundry" {
  source              = "./modules/ai_foundry"
  foundry_name        = "${local.base_name}-ai-foundry"
  project_name        = "${local.base_name}-ai-project"
  location            = var.region
  resource_group_name = azurerm_resource_group.this.name
  plant_code          = var.plant_code
}

module "rbac" {
  source                 = "./modules/rbac"
  foundry_principal_id   = module.ai_foundry.foundry_principal_id
  project_principal_id   = module.ai_foundry.project_principal_id
  foundry_resource_id    = module.ai_foundry.foundry_id
  ai_search_id           = module.ai_search.id
  ai_search_principal_id = module.ai_search.principal_id
  storage_account_id     = module.storage_account.id
}

# ---------------------------------------------------------------------------
# Connections (AI Search) — on the Foundry resource, not on a Hub
# ---------------------------------------------------------------------------
resource "azapi_resource" "search_connection" {
  type                      = "Microsoft.CognitiveServices/accounts/connections@2025-04-01-preview"
  name                      = "ai-search-connection"
  parent_id                 = module.ai_foundry.foundry_id
  schema_validation_enabled = false

  body = {
    properties = {
      category      = "CognitiveSearch"
      authType      = "AAD"
      isSharedToAll = true
      target        = module.ai_search.endpoint
      metadata = {
        ApiType    = "Azure"
        ResourceId = module.ai_search.id
      }
    }
  }
}

# ---------------------------------------------------------------------------
# Foundry IQ project connection (MCP) — attaches Search knowledge base to the
# Foundry project so agents can use knowledge_base_retrieve.
# ---------------------------------------------------------------------------
resource "azapi_resource" "foundry_iq_kb_connection" {
  type                      = "Microsoft.CognitiveServices/accounts/projects/connections@2025-10-01-preview"
  name                      = "foundry-iq-kb-connection"
  parent_id                 = module.ai_foundry.project_id
  schema_validation_enabled = false

  body = {
    properties = {
      authType      = "ProjectManagedIdentity"
      category      = "RemoteTool"
      isSharedToAll = true
      target        = "${module.ai_search.endpoint}/knowledgebases/${module.ai_search.knowledge_base_name}/mcp?api-version=2026-05-01-preview"
      audience      = "https://search.azure.com/"
      metadata = {
        ApiType = "Azure"
      }
    }
  }
}

# ---------------------------------------------------------------------------
# Fabric IQ (OneLake Catalog) project connection — points to the Fabric
# Data Agent MCP endpoint so declarative agents can use fabric_iq_preview.
# ---------------------------------------------------------------------------
resource "azapi_resource" "fabric_iq_data_agent_connection" {
  type                      = "Microsoft.CognitiveServices/accounts/projects/connections@2025-10-01-preview"
  name                      = "fabric-iq-data-agent-connection"
  parent_id                 = module.ai_foundry.project_id
  schema_validation_enabled = false

  body = {
    properties = {
      authType      = "UserEntraToken"
      category      = "RemoteTool"
      isSharedToAll = false
      target        = local.fabric_data_agent_mcp_target
      audience      = "https://api.fabric.microsoft.com"
      metadata = {
        type = "fabric_iq_preview"
      }
    }
  }
}

# ---------------------------------------------------------------------------
# Work IQ project connection — connects Maintenance and Plant Manager agents
# to Microsoft 365 work management (Planner, Tasks, work orders).
# Only provisioned when work_iq_connection_target is set.
# ---------------------------------------------------------------------------
resource "azapi_resource" "work_iq_connection" {
  count                     = local.work_iq_connection_target != "" ? 1 : 0
  type                      = "Microsoft.CognitiveServices/accounts/projects/connections@2025-10-01-preview"
  name                      = "work-iq-connection"
  parent_id                 = module.ai_foundry.project_id
  schema_validation_enabled = false

  body = {
    properties = {
      authType      = "UserEntraToken"
      category      = "RemoteTool"
      isSharedToAll = false
      target        = local.work_iq_connection_target
      audience      = "https://work.microsoft.com"
      metadata = {
        type = "work_iq_preview"
      }
    }
  }
}
