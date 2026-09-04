output "client_id" {
  description = "Application (client) ID of the Work IQ Entra app registration"
  value       = azuread_application.work_iq.client_id
}

output "client_secret" {
  description = "Client secret value for the Work IQ Entra app registration (sensitive)"
  value       = azuread_application_password.work_iq.value
  sensitive   = true
}

output "object_id" {
  description = "Object ID of the Work IQ Entra application"
  value       = azuread_application.work_iq.id
}

output "service_principal_object_id" {
  description = "Object ID of the Work IQ Entra app's service principal"
  value       = azuread_service_principal.work_iq.object_id
}
