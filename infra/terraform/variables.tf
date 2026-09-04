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

variable "enable_work_iq_connection" {
  type        = bool
  description = "Whether to provision the Work IQ Entra app registration and the Foundry Work IQ OAuth2/RemoteTool (MCP) project connection."
  default     = false
}

variable "work_iq_mcp_endpoint" {
  type        = string
  description = "Work IQ MCP server endpoint (target) for the Foundry OAuth2/RemoteTool connection."
  default     = "https://workiq.svc.cloud.microsoft/mcp"
}

variable "work_iq_scope" {
  type        = string
  description = "OAuth2 delegated scope requested for the Work IQ connection token."
  default     = "api://workiq.svc.cloud.microsoft/WorkIQAgent.Ask"
}

variable "work_iq_redirect_uris" {
  type        = list(string)
  description = "OAuth redirect URIs to register on the Work IQ Entra app (the Foundry connection's reply URL, e.g. https://global.consent.azure-apim.net/redirect/<connector-guid>). Foundry only generates this value after azapi_resource.work_iq_connection is created — read it via `az rest ... connections/work-iq-connection` (properties.redirectUrl) and add it here in a follow-up apply."
  default     = []
}
