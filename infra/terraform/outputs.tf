output "connection_contract" {
  description = "Engine-agnostic handoff contract"
  value = {
    tenantId                           = var.tenant_id
    subscriptionId                     = var.subscription_id
    resourceGroup                      = var.resource_group
    region                             = var.region
    workspaceId                        = local.workspace_id
    eventhouseId                       = module.eventhouse.eventhouse_id
    kqlDatabase                        = module.eventhouse.kql_database_name
    dataAgentId                        = module.data_agent.data_agent_id
    foundryEndpoint                    = module.ai_foundry.foundry_endpoint
    foundryProjectId                   = module.ai_foundry.project_id
    foundryIqProjectConnectionName     = azapi_resource.foundry_iq_kb_connection.name
    foundryFabricProjectConnectionName = azapi_resource.fabric_iq_data_agent_connection.name
    foundryWorkIqProjectConnectionName = length(azapi_resource.work_iq_connection) > 0 ? azapi_resource.work_iq_connection[0].name : ""
    aiSearchEndpoint                   = module.ai_search.endpoint
    foundryIqKnowledgeSourceName       = module.ai_search.knowledge_source_name
    foundryIqKnowledgeBaseName         = module.ai_search.knowledge_base_name
    modelDeploymentName                = module.ai_foundry.model_deployment_name
    embeddingDeploymentName            = module.ai_foundry.embedding_deployment_name
    storageAccountEndpoint             = module.storage_account.primary_blob_endpoint
    generatedAt                        = timestamp()
    schemaVersion                      = "3.0"
  }
}
