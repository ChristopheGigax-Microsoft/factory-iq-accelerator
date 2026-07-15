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

variable "knowledge_source_name" {
  type        = string
  description = "Blob knowledge source name to create on Azure AI Search"
}

variable "knowledge_base_name" {
  type        = string
  description = "Knowledge base name to create on Azure AI Search"
}

variable "storage_connection_string" {
  type        = string
  description = "Storage account connection string used by the blob knowledge source"
  sensitive   = true
}

variable "storage_container_name" {
  type        = string
  description = "Blob container name that holds customer-managed knowledge files"
  default     = "knowledge-base"
}

variable "model_deployment_name" {
  type        = string
  description = "GPT model deployment name used by the knowledge base for LLM-based retrieval reasoning"
  default     = "gpt-4o"
}

variable "foundry_endpoint" {
  type        = string
  description = "Azure AI Foundry endpoint used by the embedding model"
}

variable "embedding_deployment_name" {
  type        = string
  description = "Embedding model deployment name hosted on the Foundry resource"
}

variable "embedding_model_name" {
  type        = string
  description = "Embedding model name used by Azure AI Search vectorization"
  default     = "text-embedding-3-large"
}
