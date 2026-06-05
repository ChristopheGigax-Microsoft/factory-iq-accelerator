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
