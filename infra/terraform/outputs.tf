output "connection_contract" {
  description = "Engine-agnostic handoff contract"
  value = {
    tenantId       = var.tenant_id
    subscriptionId = var.subscription_id
    resourceGroup  = var.resource_group
    region         = var.region
    workspaceId    = module.workspace.workspace_id
    eventhouseId   = module.eventhouse.eventhouse_id
    kqlDatabase    = module.eventhouse.kql_database_name
    generatedAt    = timestamp()
    schemaVersion  = "1.0"
  }
}
