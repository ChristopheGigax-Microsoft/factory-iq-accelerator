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

  model {
    format  = "OpenAI"
    name    = "gpt-4o"
    version = "2024-11-20"
  }

  sku {
    name     = "GlobalStandard"
    capacity = 30
  }
}

# ---------------------------------------------------------------------------
# Embedding model deployment — used by Azure AI Search for vectorization
# ---------------------------------------------------------------------------
resource "azurerm_cognitive_deployment" "embedding" {
  name                 = var.embedding_deployment_name
  cognitive_account_id = azurerm_cognitive_account.foundry.id

  model {
    format  = "OpenAI"
    name    = "text-embedding-3-large"
    version = "1"
  }

  sku {
    name     = "Standard"
    capacity = 30
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
}
