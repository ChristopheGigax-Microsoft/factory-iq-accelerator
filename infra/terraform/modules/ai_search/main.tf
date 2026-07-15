terraform {
  required_providers {
    azurerm = {
      source = "hashicorp/azurerm"
    }
  }
}

resource "azurerm_search_service" "this" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = var.sku

  identity {
    type = "SystemAssigned"
  }

  semantic_search_sku          = "standard"
  local_authentication_enabled = false
}

locals {
  search_endpoint           = "https://${azurerm_search_service.this.name}.search.windows.net"
  generated_datasource_name = "${var.knowledge_source_name}-datasource"
  generated_indexer_name    = "${var.knowledge_source_name}-indexer"
  generated_skillset_name   = "${var.knowledge_source_name}-skillset"
  generated_index_name      = "${var.knowledge_source_name}-index"
}

resource "terraform_data" "blob_knowledge_source" {
  triggers_replace = [
    sha256(jsonencode({
      name                      = var.knowledge_source_name
      storage_container_name    = var.storage_container_name
      foundry_endpoint          = var.foundry_endpoint
      embedding_deployment_name = var.embedding_deployment_name
      embedding_model_name      = var.embedding_model_name
      storage_connection_hash   = sha256(var.storage_connection_string)
    }))
  ]

  input = {
    name            = var.knowledge_source_name
    search_endpoint = local.search_endpoint
  }

  provisioner "local-exec" {
    interpreter = ["PowerShell", "-NoProfile", "-NonInteractive", "-Command"]
    environment = {
      SEARCH_ENDPOINT           = local.search_endpoint
      KS_NAME                   = var.knowledge_source_name
      STORAGE_CONNECTION_STRING = var.storage_connection_string
      STORAGE_CONTAINER_NAME    = var.storage_container_name
      FOUNDRY_ENDPOINT          = var.foundry_endpoint
      EMBEDDING_DEPLOYMENT_NAME = var.embedding_deployment_name
      EMBEDDING_MODEL_NAME      = var.embedding_model_name
    }
    command = <<-EOT
      $body = @{
        name = $env:KS_NAME
        kind = 'azureBlob'
        description = "Customer-managed knowledge files stored in the $($env:STORAGE_CONTAINER_NAME) blob container."
        azureBlobParameters = @{
          connectionString = $env:STORAGE_CONNECTION_STRING
          containerName    = $env:STORAGE_CONTAINER_NAME
          folderPath       = $null
          isADLSGen2       = $false
          ingestionParameters = @{
            contentExtractionMode = 'minimal'
            embeddingModel = @{
              kind = 'azureOpenAI'
              azureOpenAIParameters = @{
                resourceUri  = $env:FOUNDRY_ENDPOINT
                deploymentId = $env:EMBEDDING_DEPLOYMENT_NAME
                modelName    = $env:EMBEDDING_MODEL_NAME
              }
            }
          }
        }
      } | ConvertTo-Json -Depth 20 -Compress

      $tmp = [System.IO.Path]::GetTempFileName()
      try {
        [System.IO.File]::WriteAllText($tmp, $body, [System.Text.UTF8Encoding]::new($false))
        az rest --method put --url "$($env:SEARCH_ENDPOINT)/knowledgesources/$($env:KS_NAME)?api-version=2026-04-01" --resource https://search.azure.com --headers "Content-Type=application/json" "Prefer=return=representation" --body "@$tmp" --output none
      }
      finally {
        Remove-Item $tmp -Force -ErrorAction SilentlyContinue
      }
    EOT
  }

  provisioner "local-exec" {
    when        = destroy
    interpreter = ["PowerShell", "-NoProfile", "-NonInteractive", "-Command"]
    environment = {
      SEARCH_ENDPOINT = self.input.search_endpoint
      KS_NAME         = self.input.name
    }
    command = "az rest --method delete --url \"$($env:SEARCH_ENDPOINT)/knowledgesources/$($env:KS_NAME)?api-version=2026-04-01\" --resource https://search.azure.com --output none"
  }
}

resource "terraform_data" "knowledge_base" {
  triggers_replace = [
    sha256(jsonencode({
      name                  = var.knowledge_base_name
      knowledge_source_name = var.knowledge_source_name
      foundry_endpoint      = var.foundry_endpoint
      model_deployment_name = var.model_deployment_name
    }))
  ]

  input = {
    name            = var.knowledge_base_name
    search_endpoint = local.search_endpoint
  }

  depends_on = [terraform_data.blob_knowledge_source]

  provisioner "local-exec" {
    interpreter = ["PowerShell", "-NoProfile", "-NonInteractive", "-Command"]
    environment = {
      SEARCH_ENDPOINT       = local.search_endpoint
      KB_NAME               = var.knowledge_base_name
      KS_NAME               = var.knowledge_source_name
      FOUNDRY_ENDPOINT      = var.foundry_endpoint
      MODEL_DEPLOYMENT_NAME = var.model_deployment_name
    }
    command = <<-EOT
      $body = @{
        name = $env:KB_NAME
        description = 'Foundry IQ knowledge base for Factory IQ documents stored in Azure Blob Storage.'
        knowledgeSources = @(
          @{
            name = $env:KS_NAME
          }
        )
        models = @(
          @{
            kind = 'azureOpenAI'
            azureOpenAIParameters = @{
              resourceUri  = $env:FOUNDRY_ENDPOINT
              deploymentId = $env:MODEL_DEPLOYMENT_NAME
              modelName    = $env:MODEL_DEPLOYMENT_NAME
            }
          }
        )
        encryptionKey = $null
      } | ConvertTo-Json -Depth 10 -Compress

      $tmp = [System.IO.Path]::GetTempFileName()
      try {
        [System.IO.File]::WriteAllText($tmp, $body, [System.Text.UTF8Encoding]::new($false))
        az rest --method put --url "$($env:SEARCH_ENDPOINT)/knowledgebases/$($env:KB_NAME)?api-version=2026-04-01" --resource https://search.azure.com --headers "Content-Type=application/json" "Prefer=return=representation" --body "@$tmp" --output none
      }
      finally {
        Remove-Item $tmp -Force -ErrorAction SilentlyContinue
      }
    EOT
  }

  provisioner "local-exec" {
    when        = destroy
    interpreter = ["PowerShell", "-NoProfile", "-NonInteractive", "-Command"]
    environment = {
      SEARCH_ENDPOINT = self.input.search_endpoint
      KB_NAME         = self.input.name
    }
    command = "az rest --method delete --url \"$($env:SEARCH_ENDPOINT)/knowledgebases/$($env:KB_NAME)?api-version=2026-04-01\" --resource https://search.azure.com --output none"
  }
}

output "id" {
  value = azurerm_search_service.this.id
}

output "name" {
  value = azurerm_search_service.this.name
}

output "endpoint" {
  value = "https://${azurerm_search_service.this.name}.search.windows.net"
}

output "principal_id" {
  value = azurerm_search_service.this.identity[0].principal_id
}

output "knowledge_source_name" {
  value = var.knowledge_source_name
}

output "knowledge_base_name" {
  value = var.knowledge_base_name
}

output "generated_index_name" {
  value = local.generated_index_name
}

output "generated_indexer_name" {
  value = local.generated_indexer_name
}

output "generated_skillset_name" {
  value = local.generated_skillset_name
}

output "generated_datasource_name" {
  value = local.generated_datasource_name
}
