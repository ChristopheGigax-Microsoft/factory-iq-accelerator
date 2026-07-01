terraform {
  required_providers {
    azurerm = {
      source = "hashicorp/azurerm"
    }
  }
}

# Foundry resource MI → AI Search: Search Index Data Reader
resource "azurerm_role_assignment" "foundry_search_reader" {
  scope                = var.ai_search_id
  role_definition_name = "Search Index Data Reader"
  principal_id         = var.foundry_principal_id
  principal_type       = "ServicePrincipal"
}

# Foundry resource MI → AI Search: Search Service Contributor (index management)
resource "azurerm_role_assignment" "foundry_search_contributor" {
  scope                = var.ai_search_id
  role_definition_name = "Search Service Contributor"
  principal_id         = var.foundry_principal_id
  principal_type       = "ServicePrincipal"
}

# Foundry resource MI → Storage Account: Storage Blob Data Reader
resource "azurerm_role_assignment" "foundry_storage_reader" {
  scope                = var.storage_account_id
  role_definition_name = "Storage Blob Data Reader"
  principal_id         = var.foundry_principal_id
  principal_type       = "ServicePrincipal"
}

# AI Search MI → Storage Account: Storage Blob Data Reader (indexer access)
resource "azurerm_role_assignment" "search_storage_reader" {
  scope                = var.storage_account_id
  role_definition_name = "Storage Blob Data Reader"
  principal_id         = var.ai_search_principal_id
  principal_type       = "ServicePrincipal"
}
