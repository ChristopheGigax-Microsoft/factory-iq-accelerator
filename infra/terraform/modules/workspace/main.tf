terraform {
  required_providers {
    fabric = {
      source = "microsoft/fabric"
    }
  }
}

resource "fabric_workspace" "this" {
  display_name = var.name
  capacity_id  = var.capacity_id
}

variable "name" {
  type = string
}

variable "capacity_id" {
  type    = string
  default = null
}

output "workspace_id" {
  value = fabric_workspace.this.id
}
