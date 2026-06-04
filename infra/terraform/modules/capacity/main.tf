terraform {
  required_providers {
    fabric = {
      source = "microsoft/fabric"
    }
  }
}

resource "fabric_capacity" "this" {
  name     = var.name
  location = var.location
  sku      = var.sku
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

output "capacity_id" {
  value = fabric_capacity.this.id
}
