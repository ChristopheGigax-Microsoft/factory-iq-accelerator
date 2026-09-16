[CmdletBinding()]
param(
    [switch]$NoColor
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:UseColor = -not $NoColor -and -not [Console]::IsOutputRedirected
$script:RepoRoot = Split-Path -Parent $PSScriptRoot
$script:TerraformRoot = Join-Path $script:RepoRoot 'infra\terraform'
$script:AzureDirectory = Join-Path $script:RepoRoot '.azure'

function Write-Styled {
    param(
        [Parameter(Mandatory)]
        [string]$Message,
        [ConsoleColor]$Color = [ConsoleColor]::Gray
    )

    if ($script:UseColor) {
        Write-Host $Message -ForegroundColor $Color
    }
    else {
        Write-Host $Message
    }
}

function Write-Banner {
    Write-Styled '       .-^-.' DarkCyan
    Write-Styled '   ____|_[]_|____' Cyan
    Write-Styled '  |  _   _   _  |   FACTORY IQ' Cyan
    Write-Styled '  |_[ ]_[ ]_[ ]_|   DEPLOYMENT WALKTHROUGH' White
    Write-Styled '      /_| |_\       Plan. Build. Run.' DarkCyan
    Write-Host
}

function Write-Section {
    param(
        [string]$Title,
        [string]$Subtitle = ''
    )

    Write-Host
    Write-Styled ('=' * 72) DarkCyan
    Write-Styled "  $Title" Cyan
    if ($Subtitle) {
        Write-Styled "  $Subtitle" DarkGray
    }
    Write-Styled ('-' * 72) DarkCyan
}

function Write-LabelValue {
    param(
        [string]$Label,
        [string]$Value,
        [ConsoleColor]$LabelColor = [ConsoleColor]::Yellow,
        [ConsoleColor]$ValueColor = [ConsoleColor]::Gray
    )

    if ($script:UseColor) {
        Write-Host "  $Label" -NoNewline -ForegroundColor $LabelColor
        Write-Host "  $Value" -ForegroundColor $ValueColor
    }
    else {
        Write-Host "  $Label  $Value"
    }
}

function Write-Status {
    param(
        [ValidateSet('RUN', 'OK', 'SKIP', 'FAIL', 'WARN')]
        [string]$Status,
        [string]$Message
    )

    $color = switch ($Status) {
        'RUN' { [ConsoleColor]::Cyan }
        'OK' { [ConsoleColor]::Green }
        'SKIP' { [ConsoleColor]::DarkGray }
        'WARN' { [ConsoleColor]::Yellow }
        'FAIL' { [ConsoleColor]::Red }
    }

    Write-Styled ("[{0,-4}] {1}" -f $Status, $Message) $color
}

function Read-Choice {
    param(
        [string]$Prompt,
        [string[]]$Options
    )

    while ($true) {
        Write-Section $Prompt
        for ($index = 0; $index -lt $Options.Count; $index++) {
            Write-LabelValue "[$($index + 1)]" $Options[$index] Yellow White
        }

        $answer = Read-Host 'Votre choix'
        $parsed = 0
        if ([int]::TryParse($answer, [ref]$parsed) -and $parsed -ge 1 -and $parsed -le $Options.Count) {
            return $parsed - 1
        }

        Write-Status FAIL "Choisissez un nombre entre 1 et $($Options.Count)."
    }
}

function Read-Confirmation {
    param(
        [string]$Prompt,
        [bool]$Default = $false
    )

    $suffix = if ($Default) { '[O/n]' } else { '[o/N]' }
    while ($true) {
        $answer = (Read-Host "$Prompt $suffix").Trim()
        if ($answer -eq '') {
            return $Default
        }

        switch -Regex ($answer) {
            '^(o|oui|y|yes)$' { return $true }
            '^(n|non|no)$' { return $false }
            default { Write-Status FAIL 'Répondez par oui ou non.' }
        }
    }
}

function Read-Value {
    param(
        [string]$Name,
        [string]$Description,
        [AllowEmptyString()]
        [string]$Default = '',
        [scriptblock]$Validate = { param($Value) -not [string]::IsNullOrWhiteSpace($Value) },
        [string]$ValidationMessage = 'La valeur est invalide.'
    )

    while ($true) {
        Write-Host
        Write-Styled "  > $Name" Yellow
        Write-Styled "    $Description" DarkGray
        $prompt = if ($Default -ne '') { "$Name [$Default]" } else { $Name }
        $answer = (Read-Host $prompt).Trim()
        if ($answer -eq '' -and $Default -ne '') {
            $answer = $Default
        }

        if (& $Validate $answer) {
            return $answer
        }

        Write-Status FAIL $ValidationMessage
    }
}

function Invoke-NativeCapture {
    param(
        [string]$Command,
        [string[]]$Arguments,
        [int[]]$AllowedExitCodes = @(0)
    )

    $output = & $Command @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    if ($AllowedExitCodes -notcontains $exitCode) {
        $detail = ($output | Out-String).Trim()
        throw "$Command a échoué (code $exitCode). $detail"
    }

    return ($output | Out-String).Trim()
}

function Invoke-NativeStreaming {
    param(
        [string]$Command,
        [string[]]$Arguments,
        [int[]]$AllowedExitCodes = @(0)
    )

    $outputLines = [System.Collections.Generic.List[string]]::new()
    & $Command @Arguments 2>&1 | ForEach-Object {
        $line = [string]$_
        $outputLines.Add($line)
        Write-Host $line
    }
    $exitCode = $LASTEXITCODE
    if ($AllowedExitCodes -notcontains $exitCode) {
        $detail = @(
            $outputLines |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
                Select-Object -Last 12
        ) -join [Environment]::NewLine
        throw "$Command a échoué avec le code $exitCode.`nDernières lignes de la commande :`n$detail"
    }

    return $exitCode
}

function Write-TerraformPlanSummary {
    param([string]$PlanPath)

    $planJson = Invoke-NativeCapture terraform @(
        "-chdir=$script:TerraformRoot",
        'show',
        '-json',
        $PlanPath
    )
    $plan = $planJson | ConvertFrom-Json
    $changes = @($plan.resource_changes | Where-Object {
            $actions = @($_.change.actions)
            -not ($actions.Count -eq 1 -and $actions[0] -in @('no-op', 'read'))
        })

    $created = @($changes | Where-Object {
            $actions = @($_.change.actions)
            $actions -contains 'create' -and $actions -notcontains 'delete'
        })
    $updated = @($changes | Where-Object {
            $actions = @($_.change.actions)
            $actions -contains 'update' -and $actions -notcontains 'delete'
        })
    $replaced = @($changes | Where-Object {
            $actions = @($_.change.actions)
            $actions -contains 'create' -and $actions -contains 'delete'
        })
    $deleted = @($changes | Where-Object {
            $actions = @($_.change.actions)
            $actions -contains 'delete' -and $actions -notcontains 'create'
        })

    Write-Section 'RÉSUMÉ DU PLAN TERRAFORM' 'Résumé calculé depuis le plan enregistré.'
    Write-LabelValue 'Créations' ([string]$created.Count) Green White
    Write-LabelValue 'Modifications' ([string]$updated.Count) Yellow White
    Write-LabelValue 'Remplacements' ([string]$replaced.Count) $(if ($replaced.Count -gt 0) { 'Red' } else { 'Green' }) White
    Write-LabelValue 'Suppressions' ([string]$deleted.Count) $(if ($deleted.Count -gt 0) { 'Red' } else { 'Green' }) White

    if ($changes.Count -eq 0) {
        Write-Status OK 'Aucun changement d''infrastructure.'
        return
    }

    Write-Host
    Write-Styled '  Ressources concernées par type' White
    $resourceTypes = @(
        $changes |
        Group-Object type |
        Sort-Object @{ Expression = 'Count'; Descending = $true }, Name
    )
    $resourceTypes |
        Select-Object -First 10 |
        ForEach-Object { Write-LabelValue $_.Name "$($_.Count) changement(s)" DarkCyan Gray }
    if ($resourceTypes.Count -gt 10) {
        Write-Styled "  ... et $($resourceTypes.Count - 10) autre(s) type(s) de ressource." DarkGray
    }

    $destructiveChanges = @($replaced + $deleted)
    if ($destructiveChanges.Count -gt 0) {
        Write-Host
        Write-Status WARN 'Le plan contient des actions destructives :'
        foreach ($change in $destructiveChanges) {
            $action = if (@($change.change.actions) -contains 'create') { 'REMPLACEMENT' } else { 'SUPPRESSION' }
            Write-Styled "       $action - $($change.address)" Red
        }
    }
    else {
        Write-Status OK 'Aucune suppression ni aucun remplacement.'
    }
}

function ConvertTo-HclString {
    param([AllowEmptyString()][string]$Value)

    return '"' + $Value.Replace('\', '\\').Replace('"', '\"') + '"'
}

function ConvertTo-HclStringList {
    param([string[]]$Values)

    return '[' + (($Values | ForEach-Object { ConvertTo-HclString $_ }) -join ', ') + ']'
}

function Show-Permissions {
    param(
        [bool]$EnableFoundry,
        [bool]$EnableWorkIq,
        [bool]$DeployAgents
    )

    Write-Section 'DROITS REQUIS' 'Les noms ci-dessous correspondent aux libellés officiels des portails.'
    Write-Styled '  Azure RBAC - portée abonnement' White
    Write-LabelValue 'Option A' 'Owner' Green White
    Write-LabelValue 'Option B' 'Contributor + Role Based Access Control Administrator' Green White
    Write-LabelValue 'Alternative RBAC' 'Contributor + User Access Administrator' DarkYellow Gray

    Write-Host
    Write-Styled '  Microsoft Fabric - ce ne sont pas des rôles Azure RBAC' White
    Write-LabelValue 'Workspace role' 'Admin, Member ou Contributor sur le workspace ciblé' Magenta White
    Write-LabelValue 'Capacity role' 'Capacity administrator si une capacité existante est administrée' Magenta White
    Write-LabelValue 'Tenant setting' 'L''identité doit être autorisée à utiliser les API Microsoft Fabric' Magenta Gray

    if ($EnableFoundry) {
        Write-Host
        Write-Styled '  Microsoft Foundry' White
        Write-LabelValue 'Azure RBAC' 'Contributor couvre la création des ressources Foundry, AI Search et Storage' Blue White
        Write-LabelValue 'Azure RBAC' 'Role Based Access Control Administrator couvre les role assignments créés par Terraform' Blue White
        Write-LabelValue 'Quota' 'Quota disponible pour gpt-4o et text-embedding-3-large dans la région' Blue Gray
    }
    if ($EnableWorkIq) {
        Write-Host
        Write-Styled '  Microsoft Entra - ce ne sont pas des rôles Azure RBAC' White
        Write-LabelValue 'Directory role' 'Application Administrator ou Cloud Application Administrator' Magenta White
        Write-LabelValue 'Admin consent' 'Un administrateur habilité doit pouvoir consentir WorkIQAgent.Ask' Magenta Gray
    }
    if ($DeployAgents) {
        Write-Host
        Write-Styled '  Publication des agents' White
        Write-LabelValue 'Azure RBAC' 'Foundry Project Manager' Blue White
        Write-Styled '    Terraform attribuera ce rôle à l''utilisateur connecté sur le projet créé.' DarkGray
    }
}

function Show-Variables {
    param(
        [bool]$CreateWorkspace,
        [bool]$EnableFoundry,
        [bool]$EnableWorkIq,
        [bool]$DeployAgents
    )

    Write-Section 'VARIABLES TERRAFORM' 'Les noms affichés sont exactement ceux de infra/terraform/variables.tf.'
    Write-LabelValue 'tenant_id' 'Tenant Azure cible'
    Write-LabelValue 'subscription_id' 'Abonnement Azure cible'
    Write-LabelValue 'region' 'Région Azure'
    Write-LabelValue 'resource_group' 'Resource group Azure'
    Write-LabelValue 'plant_code' 'Code court de l''usine'
    Write-LabelValue 'environment' 'Environnement, par exemple dev ou prod'
    Write-LabelValue 'create_fabric_workspace' $(if ($CreateWorkspace) { 'true (dérivé du choix précédent)' } else { 'false (dérivé du choix précédent)' }) Cyan Gray
    if ($CreateWorkspace) {
        Write-LabelValue 'capacity_sku' 'SKU de la nouvelle capacité Fabric'
        Write-LabelValue 'capacity_admin_members' 'UPN des administrateurs de la capacité'
    }
    else {
        Write-LabelValue 'workspace_id' 'GUID du workspace Fabric existant'
    }
    Write-LabelValue 'routing_profile_path' 'Chemin optionnel vers routes.json'
    Write-LabelValue 'enable_foundry' $(if ($EnableFoundry) { 'true (dérivé du périmètre)' } else { 'false (dérivé du périmètre)' }) Cyan Gray
    if ($EnableFoundry) {
        Write-LabelValue 'enable_work_iq_connection' $(if ($EnableWorkIq) { 'true (dérivé du choix précédent)' } else { 'false (dérivé du choix précédent)' }) Cyan Gray
    }

    if ($EnableFoundry) {
        Write-Host
        Write-Styled '  Valeurs calculées automatiquement - aucune saisie' White
        Write-LabelValue 'model_deployment_capacity' '30 unités de quota pour gpt-4o (valeur Terraform par défaut)' DarkCyan DarkGray
        Write-LabelValue 'embedding_deployment_capacity' '30 unités de quota pour text-embedding-3-large (valeur Terraform par défaut)' DarkCyan DarkGray
        Write-LabelValue 'fabric_data_agent_id' 'Output du Data Agent créé par Terraform' DarkCyan DarkGray
        Write-LabelValue 'fabric_data_agent_mcp_target' 'Construit depuis workspace_id et fabric_data_agent_id' DarkCyan DarkGray
        if ($DeployAgents) {
            Write-LabelValue 'agent_deployer_principal_id' 'Object ID Entra détecté via Azure CLI ; utilisé pour attribuer Foundry Project Manager' DarkCyan DarkGray
        }
        if ($EnableWorkIq) {
            Write-LabelValue 'work_iq_mcp_endpoint' 'Valeur par défaut définie dans variables.tf' DarkCyan DarkGray
            Write-LabelValue 'work_iq_scope' 'Valeur par défaut définie dans variables.tf' DarkCyan DarkGray
        }
    }
}

function Add-RightResult {
    param(
        [System.Collections.Generic.List[object]]$Results,
        [ValidateSet('OK', 'MANQUANT', 'NON VERIFIABLE')]
        [string]$Status,
        [string]$Check,
        [string]$Detail
    )

    $Results.Add([pscustomobject]@{
            Status = $Status
            Check  = $Check
            Detail = $Detail
        })
}

function Test-DeploymentRights {
    param(
        [hashtable]$Configuration,
        [pscustomobject]$Account,
        [AllowNull()]
        [string]$SignedInUserId
    )

    $results = [System.Collections.Generic.List[object]]::new()
    $subscriptionScope = "/subscriptions/$($Configuration.SubscriptionId)"

    if ($Account.tenantId -eq $Configuration.TenantId) {
        Add-RightResult $results OK 'Tenant Azure' "Tenant actif : $($Account.tenantId)"
    }
    else {
        Add-RightResult $results MANQUANT 'Tenant Azure' "L'abonnement sélectionné appartient au tenant $($Account.tenantId)."
    }

    try {
        $assignee = if ($SignedInUserId) { $SignedInUserId } else { [string]$Account.user.name }
        $roleJson = Invoke-NativeCapture az @(
            'role', 'assignment', 'list',
            '--assignee', $assignee,
            '--scope', $subscriptionScope,
            '--include-groups',
            '--include-inherited',
            '--subscription', $Configuration.SubscriptionId,
            '--output', 'json'
        )
        $roles = @($roleJson | ConvertFrom-Json)
        $roleNames = @($roles | ForEach-Object { $_.roleDefinitionName } | Sort-Object -Unique)
        $roleDetails = @(
            $roles |
                Sort-Object roleDefinitionName, scope -Unique |
                ForEach-Object {
                    $roleName = if ($_.roleDefinitionName) { $_.roleDefinitionName } else { 'Rôle personnalisé sans nom lisible' }
                    "$roleName (scope : $($_.scope))"
                }
        )
        $requiredRoles = 'Requis : Owner, ou Contributor + Role Based Access Control Administrator (User Access Administrator accepté comme alternative).'
        $checkedTarget = "Identité testée : $assignee. Scope : $subscriptionScope. Affectations directes, par groupe et héritées incluses."
        $hasOwner = $roleNames -contains 'Owner'
        $hasContributor = $roleNames -contains 'Contributor'
        $hasRbacAdmin = $roleNames -contains 'Role Based Access Control Administrator'
        $hasUserAccessAdmin = $roleNames -contains 'User Access Administrator'

        if ($hasOwner -or ($hasContributor -and ($hasRbacAdmin -or $hasUserAccessAdmin))) {
            Add-RightResult $results OK 'Azure control plane et RBAC' "$checkedTarget Rôles effectifs : $($roleDetails -join ' ; '). $requiredRoles"
        }
        elseif ($roleNames.Count -gt 0) {
            Add-RightResult $results 'NON VERIFIABLE' 'Azure control plane et RBAC' "$checkedTarget Rôles effectifs : $($roleDetails -join ' ; '). $requiredRoles La combinaison attendue n'est pas reconnue automatiquement ; vérifiez les permissions d'un éventuel rôle personnalisé."
        }
        else {
            Add-RightResult $results MANQUANT 'Azure control plane et RBAC' "$checkedTarget Azure CLI ne retourne aucune affectation RBAC effective. $requiredRoles Vérifiez le compte connecté, l'abonnement ciblé, l'activation PIM et les affectations de groupe. Diagnostic : az role assignment list --assignee `"$assignee`" --scope `"$subscriptionScope`" --include-groups --include-inherited --subscription `"$($Configuration.SubscriptionId)`" --output table"
        }
    }
    catch {
        Add-RightResult $results 'NON VERIFIABLE' 'Azure control plane et RBAC' $_.Exception.Message
    }

    $providers = @('Microsoft.Fabric')
    if ($Configuration.EnableFoundry) {
        $providers += @('Microsoft.CognitiveServices', 'Microsoft.Search', 'Microsoft.Storage')
    }

    foreach ($provider in $providers) {
        try {
            $state = Invoke-NativeCapture az @(
                'provider', 'show',
                '--namespace', $provider,
                '--subscription', $Configuration.SubscriptionId,
                '--query', 'registrationState',
                '--output', 'tsv'
            )
            if ($state -eq 'Registered') {
                Add-RightResult $results OK "Provider $provider" 'Registered'
            }
            else {
                Add-RightResult $results MANQUANT "Provider $provider" "État actuel : $state"
            }
        }
        catch {
            Add-RightResult $results 'NON VERIFIABLE' "Provider $provider" $_.Exception.Message
        }
    }

    try {
        [void](Invoke-NativeCapture az @(
                'policy', 'assignment', 'list',
                '--scope', $subscriptionScope,
                '--subscription', $Configuration.SubscriptionId,
                '--output', 'json'
            ))
        Add-RightResult $results OK 'Lecture Azure Policy' 'Les assignments sont lisibles ; Terraform plan évaluera leur impact.'
    }
    catch {
        Add-RightResult $results 'NON VERIFIABLE' 'Lecture Azure Policy' $_.Exception.Message
    }

    try {
        $denyJson = Invoke-NativeCapture az @(
            'rest',
            '--method', 'get',
            '--url', "https://management.azure.com$subscriptionScope/providers/Microsoft.Authorization/denyAssignments?api-version=2022-04-01",
            '--subscription', $Configuration.SubscriptionId,
            '--output', 'json'
        )
        $denyAssignments = @(($denyJson | ConvertFrom-Json).value)
        if ($denyAssignments.Count -eq 0) {
            Add-RightResult $results OK 'Deny assignments' 'Aucun deny assignment visible au niveau de l''abonnement.'
        }
        else {
            Add-RightResult $results 'NON VERIFIABLE' 'Deny assignments' "$($denyAssignments.Count) deny assignment(s) visible(s) ; leur impact sera confirmé par Terraform plan."
        }
    }
    catch {
        Add-RightResult $results 'NON VERIFIABLE' 'Deny assignments' $_.Exception.Message
    }

    if (-not $Configuration.CreateWorkspace) {
        try {
            $token = Invoke-NativeCapture az @(
                'account', 'get-access-token',
                '--resource', 'https://api.fabric.microsoft.com',
                '--tenant', $Configuration.TenantId,
                '--query', 'accessToken',
                '--output', 'tsv'
            )
            $headers = @{ Authorization = "Bearer $token" }
            [void](Invoke-RestMethod -Method Get -Uri "https://api.fabric.microsoft.com/v1/workspaces/$($Configuration.WorkspaceId)" -Headers $headers)
            Add-RightResult $results OK 'Accès workspace Fabric' "Workspace $($Configuration.WorkspaceId) visible."

            if ($SignedInUserId) {
                try {
                    $assignments = Invoke-RestMethod -Method Get -Uri "https://api.fabric.microsoft.com/v1/workspaces/$($Configuration.WorkspaceId)/roleAssignments" -Headers $headers
                    $directAssignment = @($assignments.value | Where-Object { $_.principal.id -eq $SignedInUserId }) | Select-Object -First 1
                    if ($directAssignment -and $directAssignment.role -in @('Admin', 'Member', 'Contributor')) {
                        Add-RightResult $results OK 'Rôle workspace Fabric' "Rôle direct : $($directAssignment.role)"
                    }
                    elseif ($directAssignment) {
                        Add-RightResult $results MANQUANT 'Rôle workspace Fabric' "Rôle direct insuffisant : $($directAssignment.role)"
                    }
                    else {
                        Add-RightResult $results 'NON VERIFIABLE' 'Rôle workspace Fabric' 'Aucun rôle direct trouvé ; un accès via groupe peut être effectif.'
                    }
                }
                catch {
                    Add-RightResult $results 'NON VERIFIABLE' 'Rôle workspace Fabric' 'Les role assignments Fabric ne sont pas lisibles avec l''identité active.'
                }
            }
            else {
                Add-RightResult $results 'NON VERIFIABLE' 'Rôle workspace Fabric' 'Object ID utilisateur indisponible.'
            }
        }
        catch {
            Add-RightResult $results MANQUANT 'Accès workspace Fabric' 'Le workspace existant n''est pas lisible avec l''identité active.'
        }
    }
    else {
        Add-RightResult $results 'NON VERIFIABLE' 'Administration Fabric' 'La création de capacité/workspace et les tenant settings ne peuvent pas être prouvés sans écriture.'
    }

    if ($Configuration.EnableFoundry) {
        try {
            [void](Invoke-NativeCapture az @(
                    'cognitiveservices', 'usage', 'list',
                    '--location', $Configuration.Region,
                    '--subscription', $Configuration.SubscriptionId,
                    '--output', 'json'
                ))
            Add-RightResult $results OK 'Lecture des quotas Foundry' "Usage lisible dans $($Configuration.Region)."
        }
        catch {
            Add-RightResult $results 'NON VERIFIABLE' 'Lecture des quotas Foundry' $_.Exception.Message
        }
        Add-RightResult $results 'NON VERIFIABLE' 'Capacité exacte des modèles' 'La disponibilité finale du modèle est confirmée par le plan et le déploiement Azure.'
    }

    if ($Configuration.EnableWorkIq) {
        Add-RightResult $results 'NON VERIFIABLE' 'Droits Entra et consentement Work IQ' 'La visibilité des rôles annuaire et les politiques de consentement varient selon le tenant.'
    }

    if ($Configuration.DeployAgents) {
        if ($SignedInUserId) {
            Add-RightResult $results OK 'Publication des agents' 'Foundry Project Manager sera attribué à l''utilisateur connecté par Terraform.'
        }
        else {
            Add-RightResult $results MANQUANT 'Publication des agents' 'Impossible de déterminer l''object ID de l''utilisateur connecté.'
        }
    }

    return $results
}

function Write-RightsReport {
    param([object[]]$Results)

    Write-Section 'RAPPORT DES DROITS' 'OK = vérifié, MANQUANT = bloquant, NON VÉRIFIABLE = confirmation requise.'
    foreach ($result in $Results) {
        $status = switch ($result.Status) {
            'OK' { 'OK' }
            'MANQUANT' { 'FAIL' }
            'NON VERIFIABLE' { 'WARN' }
        }
        Write-Status $status "$($result.Check) - $($result.Detail)"
    }
}

function Write-GeneratedTfVars {
    param([hashtable]$Configuration)

    if (-not (Test-Path $script:AzureDirectory)) {
        [void](New-Item -ItemType Directory -Path $script:AzureDirectory)
    }

    $adminMembers = ConvertTo-HclStringList @($Configuration.CapacityAdminMembers)
    $lines = @(
        "plant_code = $(ConvertTo-HclString $Configuration.PlantCode)"
        "environment = $(ConvertTo-HclString $Configuration.Environment)"
        "region = $(ConvertTo-HclString $Configuration.Region)"
        "tenant_id = $(ConvertTo-HclString $Configuration.TenantId)"
        "subscription_id = $(ConvertTo-HclString $Configuration.SubscriptionId)"
        "resource_group = $(ConvertTo-HclString $Configuration.ResourceGroup)"
        "create_fabric_workspace = $($Configuration.CreateWorkspace.ToString().ToLowerInvariant())"
        "workspace_id = $(ConvertTo-HclString $Configuration.WorkspaceId)"
        "capacity_sku = $(ConvertTo-HclString $Configuration.CapacitySku)"
        "capacity_admin_members = $adminMembers"
        "routing_profile_path = $(ConvertTo-HclString $Configuration.RoutingProfilePath)"
        "enable_foundry = $($Configuration.EnableFoundry.ToString().ToLowerInvariant())"
        "enable_work_iq_connection = $($Configuration.EnableWorkIq.ToString().ToLowerInvariant())"
        "model_deployment_capacity = $($Configuration.ModelDeploymentCapacity)"
        "embedding_deployment_capacity = $($Configuration.EmbeddingDeploymentCapacity)"
        "agent_deployer_principal_id = $(ConvertTo-HclString $Configuration.AgentDeployerPrincipalId)"
    )

    $path = Join-Path $script:AzureDirectory "deployment.$($Configuration.PlantCode).$($Configuration.Environment).tfvars"
    [IO.File]::WriteAllLines($path, $lines, [Text.UTF8Encoding]::new($false))
    return $path
}

function Set-AgentEnvironment {
    param([pscustomobject]$Contract)

    $env:AI_RUNTIME = 'cloud'
    $env:AZURE_TENANT_ID = [string]$Contract.tenantId
    $env:PROJECT_ENDPOINT = [string]$Contract.foundryProjectEndpoint
    $env:AZURE_AI_PROJECT_ENDPOINT = [string]$Contract.foundryProjectEndpoint
    $env:MODEL_DEPLOYMENT_NAME = [string]$Contract.modelDeploymentName
    $env:AI_SEARCH_ENDPOINT = [string]$Contract.aiSearchEndpoint
    $env:FOUNDRY_IQ_KNOWLEDGE_BASE_NAME = [string]$Contract.foundryIqKnowledgeBaseName
    $env:FOUNDRY_IQ_PROJECT_CONNECTION_NAME = [string]$Contract.foundryIqProjectConnectionName
    $env:FOUNDRY_FABRIC_DATA_AGENT_PROJECT_CONNECTION_NAME = [string]$Contract.foundryFabricProjectConnectionName
    $env:FOUNDRY_WORK_IQ_PROJECT_CONNECTION_NAME = [string]$Contract.foundryWorkIqProjectConnectionName
    $env:STORAGE_ACCOUNT_ENDPOINT = [string]$Contract.storageAccountEndpoint
    $env:FABRIC_DATA_AGENT_ID = [string]$Contract.dataAgentId
    $env:FABRIC_WORKSPACE_ID = [string]$Contract.workspaceId
}

function Register-FactoryIqAgents {
    $agentRoot = Join-Path $script:RepoRoot 'src\foundry-agents'
    $registrarProject = Join-Path $agentRoot 'tools\FactoryIQ.Agents.RegisterAll\FactoryIQ.Agents.RegisterAll.csproj'
    $runtimeIdentifier = [Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier

    Write-Status RUN 'Build des agents'
    [void](Invoke-NativeStreaming dotnet @('build', $registrarProject, '--runtime', $runtimeIdentifier, '--nologo'))
    Write-Status OK 'Build des agents'

    Write-Status RUN 'Enregistrement des cinq agents'
    [void](Invoke-NativeStreaming dotnet @(
            'run',
            '--no-build',
            '--runtime', $runtimeIdentifier,
            '--project', $registrarProject,
            '--',
            '--register-only'
        ))
    Write-Status OK 'Les cinq agents sont enregistrés'

    Write-Status RUN 'Vérification des outils persistés dans Foundry'
    [void](Invoke-NativeStreaming dotnet @(
            'run',
            '--no-build',
            '--runtime', $runtimeIdentifier,
            '--project', $registrarProject,
            '--',
            '--verify-only'
        ))
    Write-Status OK 'Les outils des cinq agents sont vérifiés'
}

function Invoke-FactoryIqDeployment {
    Write-Banner

    if ($PSVersionTable.PSVersion.Major -lt 7) {
        throw "PowerShell 7 minimum est requis ; version détectée : $($PSVersionTable.PSVersion)."
    }

    $scopeIndex = Read-Choice 'Que souhaitez-vous déployer ?' @(
        'Fabric uniquement'
        'Fabric + Foundry et les services Azure AI'
    )
    $enableFoundry = $scopeIndex -eq 1
    $enableWorkIq = $enableFoundry -and (Read-Confirmation 'Activer la connexion Work IQ ?')
    $deployAgents = $enableFoundry -and (Read-Confirmation 'Enregistrer les cinq agents après l''infrastructure ?' $true)
    $createWorkspace = (Read-Choice 'Workspace Fabric :' @(
            'Utiliser un workspace existant'
            'Créer la capacité et le workspace'
        )) -eq 1

    Show-Permissions -EnableFoundry $enableFoundry -EnableWorkIq $enableWorkIq -DeployAgents $deployAgents
    if (-not (Read-Confirmation 'Avez-vous pris connaissance de ces droits requis ?')) {
        Write-Status SKIP 'Walkthrough annulé avant toute vérification ou modification.'
        return
    }

    $requiredCommands = @('az', 'terraform')
    if ($deployAgents) {
        $requiredCommands += 'dotnet'
    }
    foreach ($command in $requiredCommands) {
        if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
            throw "Outil requis introuvable : $command"
        }
    }

    $terraformVersion = Invoke-NativeCapture terraform @('version', '-json') | ConvertFrom-Json
    if ([version]$terraformVersion.terraform_version -lt [version]'1.6.0') {
        throw "Terraform 1.6.0 minimum est requis ; version détectée : $($terraformVersion.terraform_version)."
    }
    if ($deployAgents) {
        $dotnetVersion = Invoke-NativeCapture dotnet @('--version')
        if ([version]$dotnetVersion -lt [version]'10.0.0') {
            throw ".NET 10 minimum est requis ; version détectée : $dotnetVersion."
        }
    }

    try {
        $currentAccount = Invoke-NativeCapture az @('account', 'show', '--output', 'json') | ConvertFrom-Json
    }
    catch {
        throw 'Azure CLI n''est pas authentifié. Exécutez az login puis relancez le walkthrough.'
    }

    Show-Variables -CreateWorkspace $createWorkspace -EnableFoundry $enableFoundry -EnableWorkIq $enableWorkIq -DeployAgents $deployAgents
    Write-Host
    Write-Styled '  Contexte Azure CLI détecté - information uniquement' White
    Write-LabelValue 'tenant_id détecté' ([string]$currentAccount.tenantId) DarkCyan DarkGray
    Write-LabelValue 'subscription_id détecté' ([string]$currentAccount.id) DarkCyan DarkGray
    Write-Styled '  Ces valeurs ne sont pas préremplies : saisissez explicitement la cible du déploiement.' DarkGray

    $guidValidator = { param($Value) $parsed = [guid]::Empty; [guid]::TryParse($Value, [ref]$parsed) -and $parsed -ne [guid]::Empty }
    $slugValidator = { param($Value) $Value -match '^[a-z0-9]+(?:-[a-z0-9]+)*$' }
    $regionValidator = { param($Value) $Value -match '^[a-z0-9]+$' }
    $resourceGroupValidator = { param($Value) $Value -match '^[-\w._()]+$' -and $Value.Length -le 90 }
    $tenantId = Read-Value 'tenant_id' 'GUID du tenant Azure cible. La saisie est obligatoire.' '' $guidValidator 'Saisissez un GUID de tenant valide.'
    $subscriptionId = Read-Value 'subscription_id' 'GUID de l''abonnement Azure cible. La saisie est obligatoire.' '' $guidValidator 'Saisissez un GUID d''abonnement valide.'
    try {
        $selectedAccount = Invoke-NativeCapture az @('account', 'show', '--subscription', $subscriptionId, '--output', 'json') | ConvertFrom-Json
    }
    catch {
        throw "L'identité active n'a pas accès à l'abonnement $subscriptionId."
    }

    $region = Read-Value 'region' 'Nom de région Azure sans espace, par exemple westeurope.' 'westeurope' $regionValidator 'Utilisez un nom de région Azure sans espace.'
    $plantCode = Read-Value 'plant_code' 'Identifiant court en minuscules, par exemple plant1.' 'plant1' $slugValidator 'Utilisez des minuscules, chiffres et tirets simples.'
    $environment = Read-Value 'environment' 'Environnement cible, par exemple dev, test ou prod.' 'dev' $slugValidator 'Utilisez des minuscules, chiffres et tirets simples.'
    $resourceGroup = Read-Value 'resource_group' 'Nom du resource group Azure.' "rg-fiq-$plantCode-$environment" $resourceGroupValidator 'Le nom du resource group est invalide.'

    $workspaceId = ''
    $capacitySku = 'F2'
    $capacityAdminMembers = @()
    if ($createWorkspace) {
        $capacitySku = Read-Value 'capacity_sku' 'SKU Fabric, par exemple F2 ou F4.' 'F2' { param($Value) $Value -match '^F[0-9]+$' } 'Utilisez un SKU Fabric tel que F2 ou F4.'
        $defaultAdmin = if ($selectedAccount.user.type -eq 'user') { [string]$selectedAccount.user.name } else { '' }
        $admins = Read-Value 'capacity_admin_members' 'UPN séparés par des virgules.' $defaultAdmin {
            param($Value)
            $items = @($Value -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
            $items.Count -gt 0 -and @($items | Where-Object { $_ -notmatch '^[^@\s]+@[^@\s]+\.[^@\s]+$' }).Count -eq 0
        } 'Saisissez au moins une adresse UPN valide.'
        $capacityAdminMembers = @($admins -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    }
    else {
        $workspaceId = Read-Value 'workspace_id' 'GUID du workspace Fabric existant.' '' $guidValidator 'Saisissez un GUID de workspace valide.'
    }

    $routingProfilePath = Read-Value 'routing_profile_path' 'Chemin optionnel vers routes.json ; Entrée pour aucun.' '' {
        param($Value)
        [string]::IsNullOrWhiteSpace($Value) -or (Test-Path $Value -PathType Leaf)
    } 'Le fichier indiqué est introuvable.'
    if ($routingProfilePath -ne '') {
        $routingProfilePath = (Resolve-Path $routingProfilePath).Path
    }

    $modelCapacity = 30
    $embeddingCapacity = 30

    $signedInUserId = $null
    if ($selectedAccount.user.type -eq 'user') {
        try {
            $signedInUserId = Invoke-NativeCapture az @('ad', 'signed-in-user', 'show', '--query', 'id', '--output', 'tsv')
        }
        catch {
            Write-Status WARN 'Impossible de résoudre l''object ID de l''utilisateur via Microsoft Graph.'
        }
    }

    $configuration = @{
        TenantId                    = $tenantId
        SubscriptionId              = $subscriptionId
        Region                      = $region
        ResourceGroup               = $resourceGroup
        PlantCode                   = $plantCode
        Environment                 = $environment
        CreateWorkspace             = $createWorkspace
        WorkspaceId                 = $workspaceId
        CapacitySku                 = $capacitySku
        CapacityAdminMembers        = $capacityAdminMembers
        RoutingProfilePath          = $routingProfilePath
        EnableFoundry               = $enableFoundry
        EnableWorkIq                = $enableWorkIq
        DeployAgents                = $deployAgents
        ModelDeploymentCapacity     = $modelCapacity
        EmbeddingDeploymentCapacity = $embeddingCapacity
        AgentDeployerPrincipalId     = if ($deployAgents -and $signedInUserId) { $signedInUserId } else { '' }
    }

    Write-Section 'PRÉFLIGHT' 'Vérification des outils, accès et contraintes avant Terraform.'
    Write-Status RUN 'Vérification automatique des droits'
    $rights = @(Test-DeploymentRights -Configuration $configuration -Account $selectedAccount -SignedInUserId $signedInUserId)
    Write-RightsReport $rights
    if (@($rights | Where-Object { $_.Status -eq 'MANQUANT' }).Count -gt 0) {
        throw 'Le déploiement est bloqué par un ou plusieurs droits manquants.'
    }
    if (@($rights | Where-Object { $_.Status -eq 'NON VERIFIABLE' }).Count -gt 0) {
        if (-not (Read-Confirmation 'Confirmez-vous les points NON VERIFIABLES pour continuer ?')) {
            Write-Status SKIP 'Walkthrough annulé avant Terraform.'
            return
        }
    }

    $tfVarsPath = Write-GeneratedTfVars $configuration
    Write-Status OK "Variables écrites dans $tfVarsPath"

    Write-Status RUN 'Initialisation Terraform'
    [void](Invoke-NativeStreaming terraform @("-chdir=$script:TerraformRoot", 'init', '-input=false'))
    Write-Status OK 'Initialisation Terraform'

    $terraformWorkspace = "fiq-$plantCode-$environment"
    Write-Status RUN "Sélection du workspace Terraform isolé : $terraformWorkspace"
    [void](Invoke-NativeStreaming terraform @(
            "-chdir=$script:TerraformRoot",
            'workspace',
            'select',
            '-or-create',
            $terraformWorkspace
        ))
    Write-Status OK "State Terraform isolé : $terraformWorkspace"

    Write-Status RUN 'Validation Terraform'
    [void](Invoke-NativeStreaming terraform @("-chdir=$script:TerraformRoot", 'validate', '-no-color'))
    Write-Status OK 'Validation Terraform'

    $planPath = Join-Path $script:AzureDirectory "deployment.$plantCode.$environment.tfplan"
    Write-Status RUN 'Plan Terraform'
    [void](Invoke-NativeStreaming terraform @(
            "-chdir=$script:TerraformRoot",
            'plan',
            '-input=false',
            '-detailed-exitcode',
            "-var-file=$tfVarsPath",
            "-out=$planPath"
        ) @(0, 2))
    Write-Status OK "Plan Terraform disponible : $planPath"
    Write-TerraformPlanSummary -PlanPath $planPath

    Write-Section 'RÉCAPITULATIF DU PLAN' "Aucune valeur sensible n'est affichée."
    Write-LabelValue 'subscription_id' $subscriptionId
    Write-LabelValue 'tenant_id' $tenantId
    Write-LabelValue 'region' $region
    Write-LabelValue 'resource_group' $resourceGroup
    Write-LabelValue 'enable_foundry' $enableFoundry
    Write-LabelValue 'enable_work_iq_connection' $enableWorkIq
    Write-LabelValue 'agents' $(if ($deployAgents) { 'enregistrement demandé' } else { 'non demandés' })
    Write-Host
    if (-not (Read-Confirmation 'Appliquer exactement ce plan Terraform ?')) {
        Write-Status SKIP 'Déploiement annulé ; le plan reste disponible.'
        return
    }

    Write-Status RUN 'Application du plan Terraform'
    [void](Invoke-NativeStreaming terraform @("-chdir=$script:TerraformRoot", 'apply', '-input=false', $planPath))
    Write-Status OK 'Infrastructure déployée'

    Write-Status RUN 'Export du contrat connection.json'
    $contractJson = Invoke-NativeCapture terraform @("-chdir=$script:TerraformRoot", 'output', '-json', 'connection_contract')
    $connectionPath = Join-Path $script:RepoRoot 'connection.json'
    [IO.File]::WriteAllText($connectionPath, $contractJson, [Text.UTF8Encoding]::new($false))
    $contract = $contractJson | ConvertFrom-Json
    foreach ($requiredProperty in @('tenantId', 'subscriptionId', 'resourceGroup', 'region', 'workspaceId', 'eventhouseId', 'kqlDatabase')) {
        if ([string]::IsNullOrWhiteSpace([string]$contract.$requiredProperty)) {
            throw "connection.json ne contient pas la propriété requise $requiredProperty."
        }
    }
    Write-Status OK "Contrat exporté : $connectionPath"

    if ($deployAgents) {
        Set-AgentEnvironment $contract
        Register-FactoryIqAgents
    }
    else {
        Write-Status SKIP 'Enregistrement des agents'
    }

    Write-Host
    Write-Styled 'Factory IQ est prêt.' Green
    Write-Host "Contrat : $connectionPath"
}

if ($MyInvocation.InvocationName -ne '.') {
    try {
        Invoke-FactoryIqDeployment
    }
    catch {
        Write-Section 'ERREUR' 'Le walkthrough est arrêté ; aucun succès n''est supposé.'
        Write-Status FAIL $_.Exception.Message
        Write-Styled '  Corrigez cette erreur puis relancez le script. Aucun apply ne démarre après un échec.' DarkGray
        exit 1
    }
}
