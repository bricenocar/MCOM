Param(
    [string] [Parameter(Mandatory = $true)] $SubscriptionId,  
    [string] [Parameter(Mandatory = $true)] $ResourceGroupName,
    [string] [Parameter(Mandatory = $true)] $ResourceGroupLocation,
    [string] [Parameter(Mandatory = $true)] $Environment,
    [string] [Parameter(Mandatory = $false)] $blobStorageUrl,
    [string] [Parameter(Mandatory = $true)] $AppClientId,
    [string] [Parameter(Mandatory = $true)] $AppSecret,
    [string] [Parameter(Mandatory = $false)] $adfFunctionResourceId, 
    [Bool] [parameter(Mandatory = $false)] $runLocally=$false    
)

Write-Host "##[group] Initializing variables and configurations"

# Initialize resource group name based on environment
$RGName = "$ResourceGroupName-$Environment"
Write-Host "##[debug] Resource group: $RGName"

# Login to Azure
if($runLocally) {
    #az login 
}
Write-Host "##[debug] Run locally: $runLocally"

# Configure default subscription to work with
az account set -s $SubscriptionId
Write-Host "##[debug] Subscription Id: $SubscriptionId"

# Configure defaults for the rest of the script
az configure --defaults location=$ResourceGroupLocation group=$RGName
Write-Host "##[debug] Resource Group location: $ResourceGroupLocation"

# Set location of parameter blobStorageUrl is null
if($runLocally -eq $true) {
    $blobStorageUrl = "./armtemplates/"
}

Write-Host "##[debug] Location of arm templates: $blobStorageUrl"

# Define templates for provisioning
$loganalyticsTemplateFile = "$($blobStorageUrl)deploy-mcom-loganalytics.json"
$apimTemplateFile = "$($blobStorageUrl)deploy-mcom-apim.json"
$apimOperationsTemplateFile = "$($blobStorageUrl)deploy-mcom-apim-operations.json"
$adfTemplateFile = "$($blobStorageUrl)deploy-mcom-adf.json"
$logicappsTemplateFile = "$($blobStorageUrl)deploy-mcom-logicapps.json"
Write-Host "##[debug]Location of log analytics arm template: $loganalyticsTemplateFile"
Write-Host "##[debug]Location of api management arm template: $apimTemplateFile"
Write-Host "##[debug]Location of api management operations arm template: $apimOperationsTemplateFile"
Write-Host "##[debug]Location of azure data factory arm template: $adfTemplateFile"
Write-Host "##[debug]Location of azure Logic App arm template: $logicappsTemplateFile"

# Define parameters for provisioning
if($runLocally -eq $false) {
    $apimParametersFile = "MCOMBuild/dropdeploymentscripts/armtemplates/deploy-mcom-apim.parameters.json"
    $adfParametersFile = "MCOMBuild/dropdeploymentscripts/armtemplates/deploy-mcom-adf.parameters.json"
    $loganalyticsParametersFile = "MCOMBuild/dropdeploymentscripts/armtemplates/deploy-mcom-loganalytics.parameters.json"
    $logicappsParametersFile = "MCOMBuild/dropdeploymentscripts/armtemplates/deploy-mcom-logicapps.parameters.json"
} else {
    $apimParametersFile = "$($blobStorageUrl)deploy-mcom-apim.parameters.json"
    $adfParametersFile = "$($blobStorageUrl)deploy-mcom-adf.parameters.json"
    $loganalyticsParametersFile = "$($blobStorageUrl)deploy-mcom-loganalytics.parameters.json"
    $logicappsParametersFile = "$($blobStorageUrl)deploy-mcom-logicapps.parameters.json"
}
Write-Host "##[debug]Location of api management parameters file: $apimParametersFile"
Write-Host "##[debug]Location of azure data factory parameters file: $adfParametersFile"
Write-Host "##[debug]Location of function app parameters file: $loganalyticsParametersFile"
Write-Host "##[debug]Location of Logic app parameters file: $logicappsParametersFile"

# Predefined variables for provisioning
$today = Get-Date -Format "ddMMyy-HHmm"
$DeploymentName = "mcom-$Environment-$today"
$functionAppName = "function-mcom-$Environment"
$stgAreaStorage = "stmcomstgarea$Environment"
$apimName = "apim-mcom-$Environment"
Write-Host "##[debug]Deployment name: $DeploymentName"
Write-Host "##[endgroup]"

Write-Host "##[group] Creation of prerequisites"
# Create the azure resource if it does not exists
$rgExists = az group exists -n $RGName
if ($rgExists -eq $false) {
    Write-Host "##[command] Creating Resource group $RGName..."
    az group create -l $ResourceGroupLocation -n $RGName
}
else {
    Write-Host "##[warning]The resource group already exists, skipping creation"
}

Write-Host "##[command] Getting tenant id"
$account = az account show | ConvertFrom-Json
$tenantId = $account.tenantId
Write-Host "##[debug] Tenant id: $tenantId"

# Get function object
Write-Host "##[command] Getting function app Url"
$function = az functionapp show -g $RGName -n $functionAppName | ConvertFrom-Json
$functionAppId = "https://management.azure.com$($function.id)"
$functionAppUrl = $function.defaultHostName
Write-Host "##[debug] Function app Url: $functionAppUrl"

# Get default key from app function
Write-Host "##[command] Getting function app default key"
$functionKeys = az functionapp keys list -g $RGName -n $functionAppName | ConvertFrom-Json
$functionAppKeyGetDriveId = $functionKeys.functionKeys.default
Write-Host "##[debug] Default key obtained"

# Get staging url from storage account
Write-Host "##[command] Getting staging area id"
$stgAreaUrl = az storage account show -n $stgAreaStorage | ConvertFrom-Json
$stgAreaId = $stgAreaUrl.id
Write-Host "##[debug] Staging area id: $stgAreaId"

# Get connectionstring from storage account
Write-Host "##[command] Getting connection string for staging area"
$stAccountConn = az storage account show-connection-string -n $stgAreaStorage | ConvertFrom-Json
$connString = $stAccountConn.connectionString
Write-Host "##[debug] Connection string obtained"
Write-Host "##[endgroup]"

Write-Host "##[group] Deployment of arm templates"
Write-Host "##[command] Running deployment of log workspace template..."
# Deploy log workspace
if($runLocally -eq $false) {
    $result = az deployment group create --name "$DeploymentName-loganalytics" --template-uri $loganalyticsTemplateFile --parameters $loganalyticsParametersFile environment=$Environment | ConvertFrom-Json
} else {
    $result = az deployment group create --name "$DeploymentName-loganalytics" --template-file $loganalyticsTemplateFile --parameters $loganalyticsParametersFile environment=$Environment | ConvertFrom-Json
}

# Evaluate result from deployment
if($result.Length -gt 0 -and $result.properties.provisioningState -eq "Succeeded") {
    Write-Host "##[section] Deployment successful"    
}

# Deploy api management (deprecated)
Write-Host "##[command] Running deployment of api management template..."
if($runLocally -eq $false) {
    $result = az deployment group create --name "$DeploymentName-apim" --template-uri $apimTemplateFile --parameters $apimParametersFile environment=$Environment apimName=$apimName | ConvertFrom-Json
} else {
    $result = az deployment group create --name "$DeploymentName-apim" --template-file $apimTemplateFile --parameters $apimParametersFile environment=$Environment apimName=$apimName | ConvertFrom-Json
}

# Evaluate result from deployment
if($result.Length -gt 0 -and $result.properties.provisioningState -eq "Succeeded") {
    Write-Host "##[section] Deployment successful, waiting until apim is provisioned"    
}

# Wait until apim is provisioned
Write-Host "##[debug] Waiting until api management is successfully created" 
az apim wait --created --name $apimName

# Deploy api management apis and operations
Write-Host "##[command] Running deployment of api management operations template..."
if($runLocally -eq $false) {
    $result = az deployment group create --name "$DeploymentName-apim-operations" --template-uri $apimOperationsTemplateFile --parameters environment=$Environment functionAppId=$functionAppId | ConvertFrom-Json #principalId=$principalId
} else {
    $result = az deployment group create --name "$DeploymentName-apim-operations" --template-file $apimOperationsTemplateFile --parameters environment=$Environment functionAppId=$functionAppId | ConvertFrom-Json #principalId=$principalId
}

# Evaluate result from deployment
if($result.Length -gt 0 -and $result.properties.provisioningState -eq "Succeeded") {
    Write-Host "##[section] Deployment successful, waiting until apim is provisioned"    
}

# Deploy Azure data factory
Write-Host "##[command] Running deployment of Azure Data Factory template..."
if($runLocally -eq $false) { 
    $result = az deployment group create --name "$DeploymentName-adf" --template-uri $adfTemplateFile --parameters $adfParametersFile environment=$Environment stgAreaConnectionString=$connString triggerScope=$stgAreaId functionAppUrl=$functionAppUrl functionKey=$functionAppKeyGetDriveId adfFunctionResourceId=$adfFunctionResourceId | ConvertFrom-Json
} else {
    $result = az deployment group create --name "$DeploymentName-adf" --template-file $adfTemplateFile --parameters $adfParametersFile environment=$Environment stgAreaConnectionString=$connString triggerScope=$stgAreaId functionAppUrl=$functionAppUrl functionKey=$functionAppKeyGetDriveId adfFunctionResourceId=$adfFunctionResourceId | ConvertFrom-Json
}

# Evaluate result from deployment
if($result.Length -gt 0 -and $result.properties.provisioningState -eq "Succeeded") {
    Write-Host "##[section] Deployment successful"    
}

# Deploy Logic app
Write-Host "##[command] Running deployment of Logic app template..."
if($runLocally -eq $false) {
    $result = az deployment group create --name "$DeploymentName-logicapps" --template-uri $logicappsTemplateFile --parameters $logicappsParametersFile environment=$Environment tenantId=$tenantId appclientid=$AppClientId appsecret=$AppSecret | ConvertFrom-Json
} else {
    $result = az deployment group create --name "$DeploymentName-logicapps" --template-file $logicappsTemplateFile --parameters $logicappsParametersFile environment=$Environment tenantId=$tenantId appclientid=$AppClientId appsecret=$AppSecret | ConvertFrom-Json
}

# Evaluate result from deployment
if($result.Length -gt 0 -and $result.properties.provisioningState -eq "Succeeded") {
    Write-Host "##[section] Deployment successful"    
}
Write-Host "##[endgroup]"