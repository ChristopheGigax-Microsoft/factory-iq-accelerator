terraform {
  required_providers {
    azurerm = {
      source = "hashicorp/azurerm"
    }
  }
}

resource "azurerm_storage_account" "this" {
  name                     = var.name
  location                 = var.location
  resource_group_name      = var.resource_group_name
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_storage_container" "knowledge" {
  name                  = "knowledge-base"
  storage_account_id    = azurerm_storage_account.this.id
  container_access_type = "private"
}

output "id" {
  value = azurerm_storage_account.this.id
}

output "name" {
  value = azurerm_storage_account.this.name
}

output "primary_blob_endpoint" {
  value = azurerm_storage_account.this.primary_blob_endpoint
}

output "principal_id" {
  value = azurerm_storage_account.this.identity[0].principal_id
}
