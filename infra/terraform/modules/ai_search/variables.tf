variable "name" {
  type        = string
  description = "AI Search service name"
}

variable "location" {
  type        = string
  description = "Azure region"
}

variable "resource_group_name" {
  type        = string
  description = "Resource group name"
}

variable "sku" {
  type        = string
  description = "AI Search SKU"
  default     = "standard"
}
