terraform {
  required_providers {
    fabric = {
      source = "microsoft/fabric"
    }
  }
}

resource "fabric_eventstream" "this" {
  display_name = var.name
  workspace_id = var.workspace_id
  format       = "Default"

  definition = {
    "eventstream.json" = {
      source = abspath(var.definition_path)
    }
  }
}

variable "name" {
  type = string
}

variable "workspace_id" {
  type = string
}

variable "definition_path" {
  type = string
}

output "eventstream_id" {
  value = fabric_eventstream.this.id
}
