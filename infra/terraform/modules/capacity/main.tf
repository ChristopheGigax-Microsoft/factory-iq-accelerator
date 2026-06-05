terraform {
  required_providers {
    azapi = {
      source = "Azure/azapi"
    }
    fabric = {
      source = "microsoft/fabric"
    }
  }
}

resource "azapi_resource" "this" {
  type      = "Microsoft.Fabric/capacities@2023-11-01"
  name      = var.name
  parent_id = var.resource_group_id
  location  = var.location

  body = {
    sku = {
      name = var.sku
      tier = "Fabric"
    }
    properties = {
      administration = {
        members = var.admin_members
      }
    }
  }
}

data "fabric_capacity" "this" {
  display_name = var.name

  depends_on = [azapi_resource.this]
}

variable "name" {
  type = string
}

variable "location" {
  type = string
}

variable "sku" {
  type = string
}

variable "resource_group_id" {
  type = string
}

variable "admin_members" {
  type = list(string)
}

output "capacity_id" {
  value = data.fabric_capacity.this.id
}
