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

# Prepare Deploy templates
$logicappsTemplateFile = "$($armLocation)/deploy-mcom-logicapps-provisioning.json"
Write-Host "##[debug]Location of logicapps arm template: $logicappsTemplateFile"

# Prepare parameters
$logicappsParametersFile = "$($armLocation)/deploy-mcom-logicapps-provisioning.parameters.json"
Write-Host "##[debug]Location of logicapps arm parameters file: $logicappsParametersFile"

# Initialize variables to use
$today = Get-Date -Format "ddMMyy-HHmm"
$DeploymentName = "mcom-$Environment-$today"
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
    $result = az deployment group create --name "$DeploymentName-logicapps" --template-file $logicappsTemplateFile --parameters $logicappsParametersFile environment=$Environment | ConvertFrom-Json
} else {    
    $result = az deployment group create --name "$DeploymentName-logicapps" --template-file $logicappsTemplateFile --parameters $logicappsParametersFile environment=$Environment | ConvertFrom-Json
}

# Evaluate result from deployment
Write-Host "##[section] result of provisioning state: $($result.properties.provisioningState)."
if($null -ne $result.properties -and ($result.properties.provisioningState -eq "Succeeded" -or $result.properties.provisioningState -eq "Accepted")) {
    Write-Host "##[section] Deployment successful"
} else {
    Write-Host "##[error] Deployment failed"
    Write-Host "##[error] $result"
    exit 1
}
Write-Host "##[endgroup]"