variable "plant_code" {
  type        = string
  description = "Plant code identifier"
}

variable "environment" {
  type        = string
  description = "Environment name, for example dev/test/prod"
}

variable "region" {
  type        = string
  description = "Azure region"
}

variable "capacity_sku" {
  type        = string
  description = "Fabric capacity SKU"
  default     = "F2"
}

variable "capacity_admin_members" {
  type        = list(string)
  description = "Fabric capacity admin identities (UPN/email)"
}

variable "tenant_id" {
  type        = string
  description = "Azure tenant ID"
}

variable "subscription_id" {
  type        = string
  description = "Azure subscription ID"
}

variable "resource_group" {
  type        = string
  description = "Azure resource group name"
}

variable "workspace_id" {
  type        = string
  description = "Existing Fabric workspace ID"
}

variable "fabric_data_agent_id" {
  type        = string
  description = "Existing Fabric Data Agent ID used by the Foundry Fabric IQ connection"
}

variable "fabric_data_agent_mcp_target" {
  type        = string
  description = "Optional full Fabric Data Agent MCP endpoint URL. If empty, Terraform uses the global api.fabric.microsoft.com endpoint."
  default     = ""
}

variable "work_iq_connection_target" {
  type        = string
  description = "Work IQ connection target URL for the Foundry project connection. Obtain this from your Microsoft 365 Work IQ service endpoint (e.g. https://work.microsoft.com). Leave empty to skip Work IQ connection provisioning."
  default     = ""
}
