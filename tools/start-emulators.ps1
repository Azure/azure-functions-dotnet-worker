Import-Module "$env:ProgramFiles\Azure Cosmos DB Emulator\PSModules\Microsoft.Azure.CosmosDB.Emulator"

Write-Host "Starting CosmosDB emulator with -NoWait"
Start-CosmosDbEmulator -NoWait
$cosmosStatus = Get-CosmosDbEmulatorStatus
Write-Host "Cosmos status: $cosmosStatus"

Write-Host "Starting Storage emulator"
& "$env:ProgramFiles(x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" "init" "/server" "(localdb)\MsSqlLocalDb"
& "$env:ProgramFiles(x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" "start"