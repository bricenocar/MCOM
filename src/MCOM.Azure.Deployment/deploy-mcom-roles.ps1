# RUN THIS SCRIPT LOCALLY BEFORE PROVISIONING TEMPLATES VIA PIPELINE IN AZURE DEVOPS. LOGIN AS SUBSCRIPTION OWNER AT LEAST
Param(
    [string] [Parameter(Mandatory = $true)] $SubscriptionId,
    [string] [Parameter(Mandatory = $true)] $ResourceGroupName,
    [string] [Parameter(Mandatory = $true)] $servicePrincipalAppId,
    [string] [Parameter(Mandatory = $true)] $Environment,
    [string] [Parameter(Mandatory = $false)] $blobStorageUrl,
    [Bool] [parameter(Mandatory = $false)] $runLocally=$false
)
Write-Host "##[group]Initializing variables and configurations"
# Initialize resource group name based on environment
$RGName = "$ResourceGroupName-$Environment"
Write-Host "##[debug]Resource group: $RGName"
# Login to Azure
if($runLocally) {
    az login 
}
Write-Host "##[debug]Run locally: $runLocally"

# Configure default subscription to work with
az account set -s $SubscriptionId
Write-Host "##[debug]Subscription Id: $SubscriptionId"

az configure --defaults group=$RGName

if($runLocally -eq $true) {
    $blobStorageUrl = "./armtemplates/"
}
Write-Host "##[debug]Location of arm templates: $blobStorageUrl"

# Build arm template file location
$rolesTemplateFile = "$($blobStorageUrl)deploy-mcom-roles.json"
Write-Host "##[debug]Location of roles arm template: $rolesTemplateFile"

# Build arm template parameters location
$rolesParametersFile = "$($blobStorageUrl)deploy-mcom-roles.parameters.json"
Write-Host "##[debug]Location of roles arm parameters: $rolesParametersFile"

# Predefined variables for provisioning
$today = Get-Date -Format "ddMMyy-HHmm"
$DeploymentName = "mcom-$Environment-$today"
Write-Host "##[debug]Deployment name: $DeploymentName"
Write-Host "##[endgroup]"

# Check existence of MCOM-Deployer role definition
########CHANGE TO az deployment sub IN PRODUCTION##################
Write-Host "##[command] Running deployment of roles template..."
if($runLocally -eq $false) {
    az deployment sub create --name "$DeploymentName-roles" --template-uri $rolesTemplateFile --parameters $rolesParametersFile environment=$Environment
} else {
    az deployment sub create --name "$DeploymentName-roles" --template-file $rolesTemplateFile --parameters $rolesParametersFile environment=$Environment
}

Write-Host "##[group]Deployment of arm templates"
# Assign role to service principal
Write-Host "##[command] Assigning roles"
$roleDef = az role assignment list --assignee $servicePrincipalAppId --scope "/subscriptions/$SubscriptionId" | ConvertFrom-Json
if($roleDef.Length -gt 0 -and $roleDef.roleDefinitionName -eq "MCOM-Deployer") {
    Write-Host "##[debug]The MCOM deployer role definition is already assigned to service principal, skipping assignation"
} else {
    $roleDef = az role definition list --name "MCOM-Deployer" | ConvertFrom-Json
    if($roleDef.Length -gt 0) {
        $roleId = $roleDef.name
        Write-Host "##[command] Assigning role $roleId to service principal $servicePrincipalAppId in subscription $SubscriptionId."
        az role assignment create --role $roleId --assignee $servicePrincipalAppId --scope "/subscriptions/$SubscriptionId"
    }
}

# Register reqiured service providers
Write-Host "##[command] Registering Microsoft.Storage provider."
az provider register --namespace 'Microsoft.Storage' --subscription $SubscriptionId
# Register Microsoft.EventGrid
Write-Host "##[command] Registering Microsoft.EventGrid provider."
az provider register --namespace 'Microsoft.EventGrid' --subscription $SubscriptionId
# Register Microsoft.ApiManagement
Write-Host "##[command] Registering Microsoft.ApiManagement provider."
az provider register --namespace 'Microsoft.ApiManagement' --subscription $SubscriptionId
# Register Microsoft.OperationalInsights
Write-Host "##[command] Registering Microsoft.OperationalInsights provider."
az provider register --namespace 'Microsoft.OperationalInsights' --subscription $SubscriptionId
# Register Microsoft.DataFactory
Write-Host "##[command] Registering Microsoft.DataFactory provider."
az provider register --namespace 'Microsoft.DataFactory' --subscription $SubscriptionId

Write-Host "##[endgroup]"
