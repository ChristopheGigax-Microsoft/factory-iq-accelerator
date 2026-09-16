output "connection_contract" {
  description = "Engine-agnostic handoff contract"
  value = {
    tenantId                           = var.tenant_id
    subscriptionId                     = var.subscription_id
    resourceGroup                      = var.resource_group
    region                             = var.region
    workspaceId                        = local.workspace_id
    workspaceCreated                   = var.create_fabric_workspace
    eventhouseId                       = module.eventhouse.eventhouse_id
    kqlDatabase                        = module.eventhouse.kql_database_name
    rawTelemetryTable                  = "RawTelemetry"
    routingProfileEnabled              = local.routing_profile_path != ""
    dataAgentId                        = module.data_agent.data_agent_id
    fabricOntologyId                   = module.ontology.ontology_id
    fabricOntologyName                 = module.ontology.ontology_name
    deploymentScope                    = var.enable_foundry ? "FabricAndFoundry" : "FabricOnly"
    foundryEndpoint                    = var.enable_foundry ? module.ai_foundry[0].foundry_endpoint : null
    foundryProjectEndpoint             = var.enable_foundry ? module.ai_foundry[0].project_endpoint : null
    foundryProjectId                   = var.enable_foundry ? module.ai_foundry[0].project_id : null
    foundryIqProjectConnectionName     = var.enable_foundry ? azapi_resource.foundry_iq_kb_connection[0].name : null
    foundryFabricProjectConnectionName = var.enable_foundry ? azapi_resource.fabric_iq_data_agent_connection[0].name : null
    foundryWorkIqProjectConnectionName = length(azapi_resource.work_iq_connection) > 0 ? azapi_resource.work_iq_connection[0].name : ""
    aiSearchEndpoint                   = var.enable_foundry ? module.ai_search[0].endpoint : null
    foundryIqKnowledgeSourceName       = var.enable_foundry ? module.ai_search[0].knowledge_source_name : null
    foundryIqKnowledgeBaseName         = var.enable_foundry ? module.ai_search[0].knowledge_base_name : null
    modelDeploymentName                = var.enable_foundry ? module.ai_foundry[0].model_deployment_name : null
    embeddingDeploymentName            = var.enable_foundry ? module.ai_foundry[0].embedding_deployment_name : null
    storageAccountEndpoint             = var.enable_foundry ? module.storage_account[0].primary_blob_endpoint : null
    generatedAt                        = timestamp()
    schemaVersion                      = "3.0"
  }
}
