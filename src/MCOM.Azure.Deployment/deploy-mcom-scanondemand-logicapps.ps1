Param(
    [string] [Parameter(Mandatory = $true)] $SubscriptionId,  
    [string] [Parameter(Mandatory = $true)] $ResourceGroupName,
    [string] [Parameter(Mandatory = $true)] $ResourceGroupLocation,
    [string] [Parameter(Mandatory = $true)] $Environment,
    [string] [Parameter(Mandatory = $false)] $blobStorageUrl,
    [string] [Parameter(Mandatory = $false)] $recipientEmail,
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
$logicappTemplateFile = "$($blobStorageUrl)deploy-mcom-logic-scanondemand.json"
Write-Host "##[debug]Location of logicapp arm template: $logicappTemplateFile"


# Prepare parameters
if($runLocally -eq $false) {
    $logicappsParametersFile = "ScanOnDemandBuild/dropdeploymentscripts/armtemplates/deploy-mcom-logic-scanondemand.parameters.json"
} else {
    $logicappsParametersFile = "$($blobStorageUrl)deploy-mcom-logic-scanondemand.parameters.json"
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
# Deploy Logic apps
Write-Host "##[command] Running deployment of Logic app template..."
if($runLocally -eq $false) {
    $result = az deployment group create --name "$DeploymentName-logicapps" --template-uri $logicappTemplateFile --parameters $logicappsParametersFile environment=$Environment scanprovider_email=$recipientEmail | ConvertFrom-Json
} else {
    $result = az deployment group create --name "$DeploymentName-logicapps" --template-file $logicappTemplateFile --parameters $logicappsParametersFile environment=$Environment scanprovider_email=$recipientEmail| ConvertFrom-Json
}

# Evaluate result from deployment
if($result.Length -gt 0 -and $result.properties.provisioningState -eq "Succeeded") {
    Write-Host "##[section] Deployment successful"
}