# -----------------------------------------------------------------------------
# Work IQ Entra app registration (confidential client, delegated / on-behalf-of)
#
# Microsoft Learn "Work IQ" tool doc mandates a Bring-your-own Entra app for
# Work IQ connections: only delegated OAuth2 (On-Behalf-Of) is supported, app-only
# / managed identity auth is NOT supported. This module creates that app, its
# service principal, requests the delegated WorkIQAgent.Ask permission, grants
# tenant-wide admin consent, and issues a client secret for the Foundry OAuth2
# connection to use.
#
# Prerequisite (one-time, tenant-wide, done manually by a Global Admin — not
# managed here because it targets Microsoft's first-party app, not ours):
#   POST https://graph.microsoft.com/v1.0/servicePrincipals
#     { "appId": "fdcc1f02-fc51-4226-8753-f668596af7f7" }
# -----------------------------------------------------------------------------

resource "time_rotating" "work_iq_secret" {
  rotation_rfc3339 = timeadd(timestamp(), var.secret_end_date_relative)

  lifecycle {
    ignore_changes = [rotation_rfc3339]
  }
}

resource "azuread_application" "work_iq" {
  display_name     = var.display_name
  sign_in_audience = "AzureADMyOrg"

  required_resource_access {
    resource_app_id = var.work_iq_service_principal_app_id

    resource_access {
      id   = var.work_iq_ask_scope_id
      type = "Scope"
    }
  }

  web {
    redirect_uris = []
  }
}

resource "azuread_service_principal" "work_iq" {
  client_id = azuread_application.work_iq.client_id
}

# Grants admin consent for the delegated WorkIQAgent.Ask scope tenant-wide, so
# end users are not individually prompted to consent.
resource "azuread_service_principal_delegated_permission_grant" "work_iq_ask" {
  service_principal_object_id          = azuread_service_principal.work_iq.object_id
  resource_service_principal_object_id = data.azuread_service_principal.work_iq_platform.object_id
  claim_values                         = ["WorkIQAgent.Ask"]
}

data "azuread_service_principal" "work_iq_platform" {
  client_id = var.work_iq_service_principal_app_id
}

resource "azuread_application_password" "work_iq" {
  application_id = azuread_application.work_iq.id
  display_name   = var.secret_display_name
  end_date       = time_rotating.work_iq_secret.rotation_rfc3339
}
