variable "foundry_name" {
  type        = string
  description = "Name of the AI Foundry resource (CognitiveServices/accounts)"
}

variable "project_name" {
  type        = string
  description = "Name of the Foundry project"
}

variable "model_deployment_name" {
  type        = string
  description = "Name of the model deployment"
  default     = "gpt-4o"
}

variable "location" {
  type        = string
  description = "Azure region"
}

variable "resource_group_name" {
  type        = string
  description = "Resource group name"
}

variable "plant_code" {
  type        = string
  description = "Plant code identifier"
}

variable "ai_search_name" {
  type        = string
  description = "AI Search service name"
}

variable "ai_search_id" {
  type        = string
  description = "AI Search service resource ID"
}
