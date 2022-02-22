
Param(
    [string] [Parameter(Mandatory = $true)] $objectId # IMPORTANT: This has to be set with the object id of the managed identity you need to give permissions to
)

Write-Host "##[group]Creation of role assignments for SharePoint, Graph and App Insights"

# Login to Azure
az login

#$objectId = "1efb3293-22a3-41f2-bb30-c1d8c2f45010"

# Get identity from Office 365 instance
Write-Host "##[command] Getting Office 365 app identity."
$sharepointOnlineIdentity = az ad sp list --display-name 'Office 365 SharePoint Online' | ConvertFrom-Json
$sharePointPrincipalId = $sharepointOnlineIdentity.objectId
Write-Host "##[debug]Office 365 SharePoint Online principal id: $sharePointPrincipalId"

# Get identity from Graph instance
Write-Host "##[command] Getting Microsoft Graph app identity."
$graphIdentity = az ad sp list --display-name 'Microsoft Graph' | ConvertFrom-Json
$graphPrincipalId = $graphIdentity[0].objectId
Write-Host "##[debug]Microsoft Graph principal id: $graphPrincipalId"

# Get identity from App Insights    
Write-Host "##[command] Getting Application Insights API app identity."
$appInsightsIdentity = az ad sp list --display-name 'Application Insights API' | ConvertFrom-Json
$appInsightsPrincipalId = $appInsightsIdentity[0].objectId
Write-Host "##[debug]Application Insights API principal id: $appInsightsPrincipalId"

# Get permissions IDs
$filesReadWriteAllId = "75359482-378d-4052-8f01-80520e7db3cd"
#$termStoreReadAll = "2a8d57a5-4090-4a41-bf1c-3c621d2ccad3"  
$sitesFullControlAllId = "678536fe-1083-478a-9c59-b99265e6b0d3"
$termStoreReadWriteAll="c8e3537c-ec53-43b9-bed3-b2bd3617ae97"
$sitesReadAll = "332a536c-c7ef-4017-ab91-336970924f0d"
#$spSitesReadAll = "d13f72ca-a275-4b96-b789-48ebcc4da984"
$readApplicationInsights = "3c63f9fe-1706-42a7-9f53-25b47753d668" 

# If running locally and you have enough permissions then uncomment these line
Write-Host "##[command] Adding permissions required to the managed identity object."
Connect-AzureAD
New-AzureADServiceAppRoleAssignment -ObjectId $objectId -PrincipalId $objectId -ResourceId $sharePointPrincipalId -Id $sitesFullControlAllId
New-AzureADServiceAppRoleAssignment -ObjectId $objectId -PrincipalId $objectId -ResourceId $sharePointPrincipalId -Id $termStoreReadWriteAll
New-AzureADServiceAppRoleAssignment -ObjectId $objectId -PrincipalId $objectId -ResourceId $graphPrincipalId -Id $filesReadWriteAllId
New-AzureADServiceAppRoleAssignment -ObjectId $objectId -PrincipalId $objectId -ResourceId $graphPrincipalId -Id $sitesReadAll
New-AzureADServiceAppRoleAssignment -ObjectId $objectId -PrincipalId $objectId -ResourceId $appInsightsPrincipalId -Id $readApplicationInsights
Write-Host "##[debug]Assignation completed"

    
Write-Host "##[endgroup]"