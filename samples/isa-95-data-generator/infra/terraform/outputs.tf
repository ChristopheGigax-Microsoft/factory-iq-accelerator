output "iothub_name" {
  description = "IoT Hub name"
  value       = azurerm_iothub.this.name
}

output "iothub_hostname" {
  description = "IoT Hub device endpoint hostname"
  value       = azurerm_iothub.this.hostname
}

output "function_app_name" {
  description = "Function App name — pass to 'func azure functionapp publish' to deploy the generator code"
  value       = azurerm_windows_function_app.this.name
}

output "function_app_url" {
  description = "Function App URL"
  value       = "https://${azurerm_windows_function_app.this.default_hostname}"
}

output "resource_group" {
  description = "Resource group containing all demo resources (shared with main accelerator)"
  value       = data.azurerm_resource_group.demo.name
}

# ── Operational commands ──────────────────────────────────────────────────────

output "cmd_deploy_code" {
  description = "Run this from the repo root to publish the generator code to Azure"
  value       = "cd samples/isa-95-data-generator/src && func azure functionapp publish ${azurerm_windows_function_app.this.name} --dotnet-isolated"
}

output "cmd_change_scenario" {
  description = "Run this to switch the active demo scenario without redeploying"
  value       = "az functionapp config appsettings set --name ${azurerm_windows_function_app.this.name} --resource-group ${data.azurerm_resource_group.demo.name} --settings DEMO_SCENARIO=<Normal|TemperatureDrift|QualityExcursion|MachineFault|ShiftChange>"
}

output "cmd_get_device_cs" {
  description = "Run this to retrieve the device connection string for local testing"
  value       = "az iot hub device-identity connection-string show --hub-name ${azurerm_iothub.this.name} --device-id ${var.device_id} --query connectionString -o tsv"
}

