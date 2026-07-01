terraform {
  required_providers {
    azurerm = {
      source = "hashicorp/azurerm"
    }
  }
}

resource "azurerm_search_service" "this" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = var.sku

  identity {
    type = "SystemAssigned"
  }

  semantic_search_sku          = "standard"
  local_authentication_enabled = false
}

output "id" {
  value = azurerm_search_service.this.id
}

output "name" {
  value = azurerm_search_service.this.name
}

output "endpoint" {
  value = "https://${azurerm_search_service.this.name}.search.windows.net"
}

output "principal_id" {
  value = azurerm_search_service.this.identity[0].principal_id
}
