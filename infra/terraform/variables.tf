variable "plant_code" {
  type        = string
  description = "Plant code identifier"

  validation {
    condition     = can(regex("^[a-z0-9]+(?:-[a-z0-9]+)*$", var.plant_code))
    error_message = "plant_code must contain lowercase letters, numbers, and single hyphens only."
  }
}

variable "environment" {
  type        = string
  description = "Environment name, for example dev/test/prod"

  validation {
    condition     = can(regex("^[a-z0-9]+(?:-[a-z0-9]+)*$", var.environment))
    error_message = "environment must contain lowercase letters, numbers, and single hyphens only."
  }
}

variable "region" {
  type        = string
  description = "Azure region"
}

variable "capacity_sku" {
  type        = string
  description = "Fabric capacity SKU"
  default     = "F2"

  validation {
    condition     = can(regex("^F[0-9]+$", var.capacity_sku))
    error_message = "capacity_sku must be a valid Fabric F SKU such as F2 or F4."
  }
}

variable "capacity_admin_members" {
  type        = list(string)
  description = "Fabric capacity admin identities (UPN/email)"
  default     = []
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
  description = "Existing Fabric workspace ID. Required when create_fabric_workspace is false."
  default     = ""
}

variable "create_fabric_workspace" {
  type        = bool
  description = "Create a Fabric capacity and workspace instead of using an existing workspace."
  default     = false
}

variable "fabric_data_agent_id" {
  type        = string
  description = "Optional existing Fabric Data Agent ID used by the Foundry Fabric IQ connection. Defaults to the Data Agent created by this deployment."
  default     = ""
}

variable "fabric_data_agent_mcp_target" {
  type        = string
  description = "Optional full Fabric Data Agent MCP endpoint URL. If empty, Terraform uses the global api.fabric.microsoft.com endpoint."
  default     = ""
}

variable "routing_profile_path" {
  type        = string
  description = "Optional path to a customer routing profile (routes.json). Leave empty for Bronze-only deployment."
  default     = ""
}

variable "enable_work_iq_connection" {
  type        = bool
  description = "Whether to provision the Work IQ Entra app registration and the Foundry Work IQ OAuth2/RemoteTool (MCP) project connection."
  default     = false
}

variable "enable_foundry" {
  type        = bool
  description = "Whether to deploy Microsoft Foundry and the required Azure AI services, storage, RBAC, and project connections."
  default     = true
}

variable "model_deployment_capacity" {
  type        = number
  description = "Capacity assigned to the GPT-4o model deployment."
  default     = 30

  validation {
    condition     = var.model_deployment_capacity > 0
    error_message = "model_deployment_capacity must be greater than zero."
  }
}

variable "embedding_deployment_capacity" {
  type        = number
  description = "Capacity assigned to the embedding model deployment."
  default     = 30

  validation {
    condition     = var.embedding_deployment_capacity > 0
    error_message = "embedding_deployment_capacity must be greater than zero."
  }
}

variable "agent_deployer_principal_id" {
  type        = string
  description = "Optional Entra object ID of the user who will publish agents. Grants Foundry Project Manager on the created project."
  default     = ""

  validation {
    condition     = trimspace(var.agent_deployer_principal_id) == "" || can(regex("^[0-9a-fA-F-]{36}$", var.agent_deployer_principal_id))
    error_message = "agent_deployer_principal_id must be empty or a GUID."
  }
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
