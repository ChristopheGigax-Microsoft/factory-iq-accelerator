terraform {
  required_providers {
    azapi = {
      source = "Azure/azapi"
    }
    azurerm = {
      source = "hashicorp/azurerm"
    }
  }
}

# ---------------------------------------------------------------------------
# AI Foundry resource (CognitiveServices/accounts with project management)
# This replaces the deprecated Hub+Project model (MachineLearningServices)
# ---------------------------------------------------------------------------
resource "azurerm_cognitive_account" "foundry" {
  name                  = var.foundry_name
  location              = var.location
  resource_group_name   = var.resource_group_name
  kind                  = "AIServices"
  sku_name              = "S0"
  custom_subdomain_name = var.foundry_name

  # Foundry v2: enables project management directly on the AI Services resource
  project_management_enabled = true

  identity {
    type = "SystemAssigned"
  }
}

# ---------------------------------------------------------------------------
# Model deployment (GPT-4o) — deployed within the Foundry resource
# ---------------------------------------------------------------------------
resource "azurerm_cognitive_deployment" "gpt4o" {
  name                 = var.model_deployment_name
  cognitive_account_id = azurerm_cognitive_account.foundry.id

  depends_on = [azapi_resource.project]

  model {
    format  = "OpenAI"
    name    = "gpt-4o"
    version = "2024-11-20"
  }

  sku {
    name     = "GlobalStandard"
    capacity = var.model_deployment_capacity
  }
}

# ---------------------------------------------------------------------------
# Embedding model deployment — used by Azure AI Search for vectorization
# ---------------------------------------------------------------------------
resource "azurerm_cognitive_deployment" "embedding" {
  name                 = var.embedding_deployment_name
  cognitive_account_id = azurerm_cognitive_account.foundry.id

  depends_on = [azurerm_cognitive_deployment.gpt4o]

  model {
    format  = "OpenAI"
    name    = "text-embedding-3-large"
    version = "1"
  }

  sku {
    name     = "Standard"
    capacity = var.embedding_deployment_capacity
  }
}

# ---------------------------------------------------------------------------
# Foundry Project — child of the Foundry resource (not a Hub)
# ---------------------------------------------------------------------------
resource "azapi_resource" "project" {
  type                      = "Microsoft.CognitiveServices/accounts/projects@2025-06-01"
  name                      = var.project_name
  parent_id                 = azurerm_cognitive_account.foundry.id
  location                  = var.location
  schema_validation_enabled = false

  body = {
    sku = {
      name = "S0"
    }
    identity = {
      type = "SystemAssigned"
    }
    properties = {
      displayName = "Factory IQ Agents"
      description = "Manufacturing agents for plant ${var.plant_code}"
    }
  }

  retry = {
    error_message_regex  = ["RequestConflict", "Another operation is in progress"]
    interval_seconds     = 10
    max_interval_seconds = 60
  }

  timeouts {
    create = "15m"
    update = "15m"
  }
}
