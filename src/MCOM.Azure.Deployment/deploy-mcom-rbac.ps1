Param(
    [string] [Parameter(Mandatory = $true)] $SubscriptionId,  
    [string] [Parameter(Mandatory = $true)] $ResourceGroupName,
    [string] [Parameter(Mandatory = $true)] $ResourceGroupLocation,
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

# Configure defaults for the rest of the script
az configure --defaults location=$ResourceGroupLocation group=$RGName
Write-Host "##[debug]Resource Group location: $ResourceGroupLocation"

# Set location of parameter blobStorageUrl is null
if($runLocally -eq $true) {
    $blobStorageUrl = "./armtemplates/"    
}
Write-Host "##[debug]Location of arm templates: $blobStorageUrl"

# Prepare Deploy templates
$rbacTemplateFile = "$($blobStorageUrl)deploy-mcom-rbac-$Environment.json"
Write-Host "##[debug]Location of RBAC arm template: $rbacTemplateFile"

# Initialize variables to use
$today = Get-Date -Format "ddMMyy-HHmm"
$DeploymentName = "mcom-rbac-$Environment-$today"
if ($Environment -eq "prod") {
    $SharePointUrl = "https://statoilsrm.sharepoint.com/"
} else {
    $SharePointUrl = "https://statoilintegrationtest.sharepoint.com/"
}

Write-Host "##[debug]Deployment name: $DeploymentName"
Write-Host "##[endgroup]"

Write-Host "##[group]Creation of prerequisites"
# Create the azure resource if it does not exists
$rgExists = az group exists -n $RGName
if ($rgExists -eq $false) {
    Write-Host "##[command] Creating Resource group $RGName..."
    az group create -l $ResourceGroupLocation -n $RGName    
}
else {
    Write-Host "##[warning]The resource group already exists, skipping creation"
}
Write-Host "##[endgroup]"

Write-Host "##[group]Assignation of roles"

# Get roles needed
$role = az role definition list --name "SQL DB Contributor" | ConvertFrom-Json
$sqlDbContributor = $role.id
Write-Host "##[debug] Got role ID of SQL DB Contributor: $sqlDbContributor"

$role = az role definition list --name "Storage Account Contributor" | ConvertFrom-Json
$stAccountContributor = $role.id
Write-Host "##[debug] Got role ID of Storage Account Contributor: $stAccountContributor"

$role = az role definition list --name "Storage Blob Data Contributor" | ConvertFrom-Json
$stBlobDataContributor = $role.id
Write-Host "##[debug] Got role ID of Storage Blob Data Contributor: $stBlobDataContributor"

# Get managed identities to assign permisions
$scanOutputSP = az ad sp list --all --filter "servicePrincipalType eq 'ManagedIdentity' and displayname eq 'logic-mcom-scan-output-$Environment'" | ConvertFrom-Json
$scanOutputObjId = $scanOutputSP.objectId
Write-Host "##[debug] Got managed identity of logic-mcom-scan-output-$Environment : $scanOutputObjId"

# Get managed identities to assign permisions
$scanInputSP = az ad sp list --all --filter "servicePrincipalType eq 'ManagedIdentity' and displayname eq 'logic-mcom-scan-input-$Environment'" | ConvertFrom-Json
$scanInputObjId = $scanInputSP.objectId
Write-Host "##[debug] Got managed identity of logic-mcom-scan-input-$Environment : $scanInputObjId"

# Get scopes to assign permissions
$resource = az sql server show --name "sql-mcom-gov-$Environment" | ConvertFrom-Json
$sqlServerId = $resource.id
Write-Host "##[debug] Got scope sql-mcom-gov-$Environment : $sqlServerId"

$resource = az storage account show --name "stmcomstgarea$Environment" | ConvertFrom-Json
$stAccountId = $resource.id
Write-Host "##[debug] Got scope stmcomstgarea$Environment : $stAccountId"

# Assign roles to scan output logic app
az role assignment create --assignee-object-id $scanOutputObjId --assignee-principal-type "ServicePrincipal" --role $sqlDbContributor --scope $sqlServerId
az role assignment create --assignee-object-id $scanOutputObjId --assignee-principal-type "ServicePrincipal" --role $stAccountContributor --scope $stAccountId
az role assignment create --assignee-object-id $scanOutputObjId --assignee-principal-type "ServicePrincipal" --role $stBlobDataContributor --scope $stAccountId

# Assign roles to scan output logic app
az role assignment create --assignee-object-id $scanInputObjId --assignee-principal-type "ServicePrincipal" --role $sqlDbContributor --scope $sqlServerId

Write-Host "##[endgroup]"