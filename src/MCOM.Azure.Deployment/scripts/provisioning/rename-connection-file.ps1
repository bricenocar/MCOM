Param(
    [string] [Parameter(Mandatory = $true)] $pathToLogicAppProjectDirectory
)

Write-Host "Renaming connection file";
Write-Host "Path to file $($pathToLogicAppProjectDirectory)/mcom_workflows/LogicApp/connections.json";
Rename-Item -Path "$($pathToLogicAppProjectDirectory)/mcom_workflows/LogicApp/connections.json" -NewName "connections_localfile.json";
Rename-Item -Path "$($pathToLogicAppProjectDirectory)/mcom_workflows/LogicApp/connections_azure.json" -NewName "connections.json";