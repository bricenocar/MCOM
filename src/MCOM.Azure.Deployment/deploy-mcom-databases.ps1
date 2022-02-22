Param(
    [string] [Parameter(Mandatory = $true)] $SubscriptionId,  
    [string] [Parameter(Mandatory = $true)] $ResourceGroupName,
    [string] [Parameter(Mandatory = $true)] $ResourceGroupLocation,
    [string] [Parameter(Mandatory = $true)] $Environment,
    [string] [Parameter(Mandatory = $true)] $user,
    [string] [Parameter(Mandatory = $true)] $pass,
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
$databasesTemplateFile = "$($blobStorageUrl)deploy-mcom-databases.json"
Write-Host "##[debug]Location of Databases arm template: $databasesTemplateFile"

# Prepare parameters
if($runLocally -eq $false) {
    $databasesParametersFile = "ScanOnDemandBuild/dropdeploymentscripts/armtemplates/deploy-mcom-databases.parameters.json"
} else {
    $databasesParametersFile = "$($blobStorageUrl)deploy-mcom-databases.parameters.json"
}
Write-Host "##[debug]Location of databases arm parameters file: $databasesParametersFile"

# Initialize variables to use
$today = Get-Date -Format "ddMMyy-HHmm"
$DeploymentName = "mcom-db-$Environment-$today"
$serverName = "sql-mcom-gov-$Environment"
$password = ConvertTo-SecureString $pass -AsPlainText -Force

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
# Deploy databases 
Write-Host "##[command] Running deployment of databases template..."
if($runLocally -eq $false) {
    $result = az deployment group create --name "$DeploymentName-databases" --template-uri $databasesTemplateFile --parameters $databasesParametersFile environment=$Environment serverName=$serverName administratorLogin=$user administratorLoginPassword=$password | ConvertFrom-Json
} else {
    $result = az deployment group create --name "$DeploymentName-databases" --template-file $databasesTemplateFile --parameters $databasesParametersFile environment=$Environment serverName=$serverName administratorLogin=$user administratorLoginPassword=$password | ConvertFrom-Json
}

# Evaluate result from deployment
if($result.Length -gt 0 -and $result.properties.provisioningState -eq "Succeeded") {
    Write-Host "##[section] Deployment successful"
}
Write-Host "##[endgroup]"