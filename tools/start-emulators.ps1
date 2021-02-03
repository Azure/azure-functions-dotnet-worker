Import-Module "$env:ProgramFiles\Azure Cosmos DB Emulator\PSModules\Microsoft.Azure.CosmosDB.Emulator"

Write-Host ""
Write-Host "---Starting CosmosDB emulator---"
$cosmosStatus = Get-CosmosDbEmulatorStatus

if ($cosmosStatus -ne "Running")
{
    Start-CosmosDbEmulator -NoWait
    Start-Sleep -Seconds 2
}

Write-Host "Cosmos status: $cosmosStatus"
Write-Host "------"
Write-Host ""
Write-Host "---Starting Storage emulator---"
& "${Env:ProgramFiles(x86)}\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" "start"
Write-Host "------"
Write-Host ""
Write-Host "---Checking CosmosDB emulator status---"
while ($cosmosStatus -ne "Running")
{
    $cosmosStatus = Get-CosmosDbEmulatorStatus
    Start-Sleep -Seconds 2
}
Write-Host "Cosmos status: $cosmosStatus"
Write-Host "------"
Write-Host ""