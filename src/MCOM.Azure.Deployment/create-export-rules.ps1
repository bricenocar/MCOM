Param(
    [string] [Parameter(Mandatory = $true)] $SubscriptionId,  
    [string] [Parameter(Mandatory = $true)] $ResourceGroupName,
    [string] [Parameter(Mandatory = $true)] $ResourceGroupLocation,
    [string] [Parameter(Mandatory = $true)] $Environment,
    [Bool] [parameter(Mandatory = $false)] $runLocally=$false
)

# Initialize resource group name based on environment
$RGName = "$ResourceGroupName-$Environment"

# Login to Azure
if($runLocally) {
    az login 
}

# Configure default subscription to work with
az account set -s $SubscriptionId

# Configure defaults for the rest of the script
az configure --defaults location=$ResourceGroupLocation group=$RGName

# Get url from storage account
$stgAreaStorage = "stmcomstgarea$Environment"
$logStorageUrl = az storage account show -n $stgAreaStorage | ConvertFrom-Json
$logStorageId = $logStorageUrl.id

# Create export rule
$workSpaceName = "log-mcom-$Environment"
az monitor log-analytics workspace data-export create --resource-group $RGName --workspace-name $workSpaceName --name "MCOM ADF export" --tables ADFActivityRun ADFPipelineRun ADFTriggerRun --destination $logStorageId

