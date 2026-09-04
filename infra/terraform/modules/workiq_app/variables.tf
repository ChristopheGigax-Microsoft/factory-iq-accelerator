variable "display_name" {
  type        = string
  description = "Display name for the confidential-client Entra app registration used for Work IQ authentication"
  default     = "FactoryIQ-WorkIQ-Connection"
}

variable "work_iq_service_principal_app_id" {
  type        = string
  description = "First-party Work IQ application (client) ID, published by Microsoft. Its service principal must already exist in the tenant (one-time Global Admin provisioning step)."
  default     = "fdcc1f02-fc51-4226-8753-f668596af7f7"
}

variable "work_iq_ask_scope_id" {
  type        = string
  description = "Object ID of the delegated WorkIQAgent.Ask oauth2PermissionScope exposed by the Work IQ service principal."
  default     = "0b1715fd-f4bf-4c63-b16d-5be31f9847c2"
}

variable "secret_display_name" {
  type        = string
  description = "Display name for the client secret credential used by the Foundry Work IQ OAuth2 connection"
  default     = "foundry-work-iq-connection"
}

variable "secret_end_date_relative" {
  type        = string
  description = "Relative expiry for the client secret (Terraform time_rotating/time_offset style duration string, e.g. 8760h for 1 year)"
  default     = "8760h"
}

variable "redirect_uris" {
  type        = list(string)
  description = "OAuth redirect URIs (Foundry connection reply URLs, e.g. https://global.consent.azure-apim.net/redirect/<connector-guid>) to register on the app's Web platform. Foundry only issues this URL after the connection is created, so it must be added in a follow-up apply once known."
  default     = []
}
