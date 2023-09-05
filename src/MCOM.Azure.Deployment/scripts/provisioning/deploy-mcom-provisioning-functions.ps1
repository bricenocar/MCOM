Param(
    [string] [Parameter(Mandatory = $true)] $SubscriptionId,  
    [string] [Parameter(Mandatory = $true)] $ResourceGroupName,
    [string] [Parameter(Mandatory = $true)] $ResourceGroupLocation,
    [string] [Parameter(Mandatory = $true)] $Environment,
    [string] [Parameter(Mandatory = $false)] $armLocation,
    [Bool] [parameter(Mandatory = $false)] $runLocally=$false,    
    [string] [Parameter(Mandatory = $false)] $password
)

Write-Host "##[group]Initializing variables and configurations"
# Initialize resource group name based on environment
$RGName = "$ResourceGroupName-provisioning-$Environment"
$mcomRGName = "$ResourceGroupName-$Environment"
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

# Set location of parameter armLocation is null
if($runLocally -eq $true) {
    $armLocation = "./armtemplates/"    
}
Write-Host "##[debug]Location of arm templates: $armLocation"

# Prepare Deploy templates
$storageTemplateFile = "$($armLocation)/deploy-mcom-storage-provisioning.json"
$functionTemplateFile = "$($armLocation)/deploy-mcom-func-provisioning.json"
Write-Host "##[debug]Location of storage arm template: $storageTemplateFile"
Write-Host "##[debug]Location of app function arm template: $functionTemplateFile"

# Prepare parameters
$funcParametersFile = "$($armLocation)/deploy-mcom-func-provisioning.parameters.json"
$storageParametersFile = "$($armLocation)/deploy-mcom-storage-provisioning.parameters.json"
Write-Host "##[debug]Location of storage arm parameters file: $storageParametersFile"
Write-Host "##[debug]Location of function app parameters file: $funcParametersFile"

# Initialize variables to use
$today = Get-Date -Format "ddMMyy-HHmm"
$DeploymentName = "mcom-$Environment-$today"
if ($Environment -eq "prod") {
    $SharePointUrl = "https://statoilsrm.sharepoint.com/"
} else {
    $SharePointUrl = "https://statoilintegrationtest.sharepoint.com/"
}

# Generate a guid for the role blob
$roleBlobGuid = "e53e75cb-ed35-44f9-b1f4-c999b8cab71f";

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
Write-Host "##[command] Running deployment of storage template..."
if($runLocally -eq $false) {    
    $result = az deployment group create --name "$DeploymentName-storage" --template-file $storageTemplateFile --parameters $storageParametersFile environment=$Environment | ConvertFrom-Json
} else {    
    $result = az deployment group create --name "$DeploymentName-storage" --template-file $storageTemplateFile --parameters $storageParametersFile environment=$Environment | ConvertFrom-Json
}

# Evaluate result from deployment
if($result.Length -gt 0 -and $result.properties.provisioningState -eq "Succeeded") {
    Write-Host "##[section] Deployment successful"
}

# Deploy the function app
Write-Host "##[command] Running deployment of function app template..."
if($runLocally -eq $false) {
    $result = az deployment group create --name "$DeploymentName-func" --template-file $functionTemplateFile --parameters $funcParametersFile environment=$Environment SharePointUrl=$SharePointUrl mcomRG=$mcomRGName roleBlobGuid=$roleBlobGuid | ConvertFrom-Json
} else {
    $result = az deployment group create --name "$DeploymentName-func" --template-file $functionTemplateFile --parameters $funcParametersFile environment=$Environment SharePointUrl=$SharePointUrl mcomRG=$mcomRGName roleBlobGuid=$roleBlobGuid | ConvertFrom-Json
}

# Evaluate result from deployment
if($null -ne $result.properties -and ($result.properties.provisioningState -eq "Succeeded" -or $result.properties.provisioningState -eq "Accepted")) {
    Write-Host "##[section] Deployment successful"
}
Write-Host "##[endgroup]"