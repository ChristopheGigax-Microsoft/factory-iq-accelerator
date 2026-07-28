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
  description = "Resource group name for the demo infrastructure"
  default     = "rg-isa95-demo"
}

variable "region" {
  type        = string
  description = "Azure region. Should match the main accelerator region to minimise latency."
  default     = "francecentral"
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
