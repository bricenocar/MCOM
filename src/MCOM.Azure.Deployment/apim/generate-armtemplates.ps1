Param(
    [string] [Parameter(Mandatory=$true)] $SubscriptionId,  
    [string] [Parameter(Mandatory=$true)] $ResourceGroupName,
    [string] [Parameter(Mandatory=$true)] $DevToolKitPath,
    [string] [Parameter(Mandatory=$true)] $apiName
)

# Login to Azure
az login

# Configure default subscription to work with
az account set -s $SubscriptionId

# Generate ARM templates for APIM
if($DevToolKitPath -ne $null -and $DevToolKitPath.Length -gt 0) {
    $folder = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($PSScriptRoot, "/apim/templates/postfile"))
    dotnet run $DevToolKitPath extract --sourceApimName "apim-eim-dev" --destinationApimName "apim-eim-tst" --resourceGroup $ResourceGroupName --fileFolder $folder --apiName $apiName --policyXMLBaseUrl ""
}