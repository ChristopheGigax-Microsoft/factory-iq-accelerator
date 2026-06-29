terraform {
  required_providers {
    fabric = {
      source = "microsoft/fabric"
    }
  }
}

resource "fabric_data_agent" "this" {
  display_name = var.name
  description  = var.description
  workspace_id = var.workspace_id
}

variable "name" {
  type        = string
  description = "Display name of the Data Agent"
}

variable "description" {
  type        = string
  description = "Description of the Data Agent"
  default     = ""
}

variable "workspace_id" {
  type        = string
  description = "Fabric workspace ID"
}

output "data_agent_id" {
  value = fabric_data_agent.this.id
}
