Import-Module "$env:ProgramFiles\Azure Cosmos DB Emulator\PSModules\Microsoft.Azure.CosmosDB.Emulator"

Write-Host "---Starting CosmosDB emulator---"
Start-CosmosDbEmulator
$cosmosStatus = Get-CosmosDbEmulatorStatus
Write-Host "Cosmos status: $cosmosStatus"
Write-Host "------"
Write-Host ""
Write-Host "---Starting Storage emulator---"
& "${Env:ProgramFiles(x86)}\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" "init" "/server" "(localdb)\MsSqlLocalDb"
& "${Env:ProgramFiles(x86)}\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" "start"
Write-Host "------"
Write-Host ""
Write-Host "---Checking CosmosDB emulator status---"
$cosmosStatus = Get-CosmosDbEmulatorStatus
Write-Host "Cosmos status: $cosmosStatus"
Write-Host "------"

