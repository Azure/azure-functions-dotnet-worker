Import-Module "$env:ProgramFiles\Azure Cosmos DB Emulator\PSModules\Microsoft.Azure.CosmosDB.Emulator"

Write-Host "Starting CosmosDB Emulator"
Start-CosmosDbEmulator
$cosmosStatus = Get-CosmosDbEmulatorStatus
Write-Host "Cosmos status: $cosmosStatus"