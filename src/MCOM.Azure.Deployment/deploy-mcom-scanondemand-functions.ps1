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
$RGName = "$ResourceGroupName-scan-$Environment"
$mcomRGName = "$ResourceGroupName-$Environment"
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
$storageTemplateFile = "$($blobStorageUrl)deploy-mcom-storage-scanondemand.json"
$functionTemplateFile = "$($blobStorageUrl)deploy-mcom-func-scanondemand.json"
Write-Host "##[debug]Location of storage arm template: $storageTemplateFile"
Write-Host "##[debug]Location of app function arm template: $functionTemplateFile"

# Prepare parameters
if($runLocally -eq $false) {
    $funcParametersFile = "ScanOnDemandBuild/dropdeploymentscripts/armtemplates/deploy-mcom-func-scanondemand.parameters.json"
    $storageParametersFile = "ScanOnDemandBuild/dropdeploymentscripts/armtemplates/deploy-mcom-storage-scanondemand.parameters.json"
} else {
    $funcParametersFile = "$($blobStorageUrl)deploy-mcom-func-scanondemand.parameters.json"
    $storageParametersFile = "$($blobStorageUrl)deploy-mcom-storage-scanondemand.parameters.json"
}
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
    $result = az deployment group create --name "$DeploymentName-storage" --template-uri $storageTemplateFile --parameters $storageParametersFile environment=$Environment | ConvertFrom-Json
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
    $result = az deployment group create --name "$DeploymentName-func" --template-uri $functionTemplateFile --parameters $funcParametersFile environment=$Environment SharePointUrl=$SharePointUrl mcomRG=$mcomRGName | ConvertFrom-Json
} else {
    $result = az deployment group create --name "$DeploymentName-func" --template-file $functionTemplateFile --parameters $funcParametersFile environment=$Environment SharePointUrl=$SharePointUrl mcomRG=$mcomRGName | ConvertFrom-Json
}

# Evaluate result from deployment
if($result.Length -gt 0 -and $result.properties.provisioningState -eq "Succeeded") {
    Write-Host "##[section] Deployment successful"
}
Write-Host "##[endgroup]"