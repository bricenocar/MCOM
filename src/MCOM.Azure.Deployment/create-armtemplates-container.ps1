Param(
    [string] [Parameter(Mandatory = $true)] $SubscriptionId,  
    [string] [Parameter(Mandatory = $true)] $ResourceGroupName,
    [string] [Parameter(Mandatory = $true)] $ResourceGroupLocation,
    [string] [Parameter(Mandatory = $true)] $Environment,
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

Write-Host "##[group]Creation of resources"
# Create storage account for arm templates
Write-Host "##[command] Creating azure storage account..."
az storage account create -n "stmcomarm$($Environment)" --sku Standard_LRS --allow-blob-public-access true
Write-Host "##[command] Getting key from account..."
$keys = az storage account keys list -n "stmcomarm$($Environment)" | ConvertFrom-Json
Write-Host "##[debug] Found $($keys.Length) account keys"
$key = $keys.Get(0)
Write-Host "##[command] Creating container if it does not exist..."
$exists = az storage container exists --account-name "stmcomarm$($Environment)" --account-key $key.value --name "armartifacts" | ConvertFrom-Json
if($exists.exists -eq $false) {
    az storage container create -n "armartifacts" --public-access blob --account-name "stmcomarm$($Environment)" --account-key $key.value
}
Write-Host "##[endgroup]"
