variable "foundry_principal_id" {
  type        = string
  description = "Principal ID of the Foundry resource managed identity"
}

variable "project_principal_id" {
  type        = string
  description = "Principal ID of the Foundry project managed identity"
}

variable "ai_search_id" {
  type        = string
  description = "Resource ID of the AI Search service"
}

variable "ai_search_principal_id" {
  type        = string
  description = "Principal ID of the AI Search managed identity"
}

variable "storage_account_id" {
  type        = string
  description = "Resource ID of the storage account"
}

variable "foundry_resource_id" {
  type        = string
  description = "Resource ID of the Foundry parent resource"
}
