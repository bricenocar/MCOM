Param(
    [string] [Parameter(Mandatory = $true)] $SubscriptionId,  
    [string] [Parameter(Mandatory = $true)] $ResourceGroupName,
    [string] [Parameter(Mandatory = $true)] $ResourceGroupLocation,
    [string] [Parameter(Mandatory = $true)] $Environment,
    [string] [Parameter(Mandatory = $false)] $blobStorageUrl,
    [Bool] [parameter(Mandatory = $false)] $runLocally=$false,    
    [string] [Parameter(Mandatory = $false)] $password
)

Write-Host "##[group]Initializing variables and configurations"
# Initialize resource group name based on environment
$RGName = "$ResourceGroupName-provisioning-$Environment"
Write-Host "##[debug]Resource group: $RGName"

# Login to Azure
if($runLocally) {
    az login --service-principal --username 7cb26d9e-0571-4f70-bf25-ca49bf5aa5cb --password $password --tenant e78a86b8-aa34-41fe-a537-9392c8870bf0
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
$logicappsTemplateFile = "$($blobStorageUrl)deploy-mcom-logicapps-provisioning.json"
Write-Host "##[debug]Location of logicapps arm template: $logicappsTemplateFile"

# Prepare parameters
if($runLocally -eq $false) {
    $logicappsParametersFile = "MCOMProvisioningService/dropdeploymentscripts/armtemplates/deploy-mcom-logicapps-provisioning.parameters.json"
} else {
    $logicappsParametersFile = "$($blobStorageUrl)deploy-mcom-logicapps-provisioning.parameters.json"
}
Write-Host "##[debug]Location of logicapps arm parameters file: $logicappsParametersFile"

# Initialize variables to use
$today = Get-Date -Format "ddMMyy-HHmm"
$DeploymentName = "mcom-$Environment-$today"
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

Write-Host "##[group]Deployment of arm templates"
# Deploy storage accounts and containers
Write-Host "##[command] Running deployment of logic apps template..."
if($runLocally -eq $false) {    
    $result = az deployment group create --name "$DeploymentName-logicapps" --template-uri $logicappsTemplateFile --parameters $logicappsParametersFile environment=$Environment | ConvertFrom-Json
} else {    
    $result = az deployment group create --name "$DeploymentName-logicapps" --template-file $logicappsTemplateFile --parameters $logicappsParametersFile environment=$Environment | ConvertFrom-Json
}

# Evaluate result from deployment
if($result.Length -gt 0 -and $result.properties.provisioningState -eq "Succeeded") {
    Write-Host "##[section] Deployment successful"
} else {
    Write-Host "##[error] Deployment failed"
    Write-Host "##[error] $result"
    exit 1
}
Write-Host "##[endgroup]"