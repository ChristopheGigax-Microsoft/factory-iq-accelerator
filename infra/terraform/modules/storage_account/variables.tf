variable "name" {
  type        = string
  description = "Storage account name (lowercase, no hyphens, max 24 chars)"
}

variable "location" {
  type        = string
  description = "Azure region"
}

variable "resource_group_name" {
  type        = string
  description = "Resource group name"
}
