variable "subscription_id" {
  type        = string
  description = "Azure subscription ID"
}

variable "tenant_id" {
  type        = string
  description = "Azure tenant ID"
}

variable "resource_group" {
  type        = string
  description = "Name of the EXISTING resource group to deploy into (same as the main accelerator RG)"
  default     = "rg-fiq-plant1-dev"
}

variable "device_id" {
  type        = string
  description = "IoT Hub device ID used by the data generator Function App"
  default     = "isa95-generator"
}

variable "demo_scenario" {
  type        = string
  description = "Active demo scenario injected as a Function App setting. Change to switch scenarios without redeploying code."
  default     = "Normal"

  validation {
    condition = contains(
      ["Normal", "TemperatureDrift", "QualityExcursion", "MachineFault", "ShiftChange"],
      var.demo_scenario
    )
    error_message = "demo_scenario must be one of: Normal, TemperatureDrift, QualityExcursion, MachineFault, ShiftChange."
  }
}
