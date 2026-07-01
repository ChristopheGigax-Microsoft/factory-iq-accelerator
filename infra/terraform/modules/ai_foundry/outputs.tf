output "foundry_id" {
  value = azurerm_cognitive_account.foundry.id
}

output "foundry_endpoint" {
  value = azurerm_cognitive_account.foundry.endpoint
}

output "foundry_principal_id" {
  value = azurerm_cognitive_account.foundry.identity[0].principal_id
}

output "project_id" {
  value = azapi_resource.project.id
}

output "model_deployment_name" {
  value = azurerm_cognitive_deployment.gpt4o.name
}
