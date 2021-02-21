Import-Module "$env:ProgramFiles\Azure Cosmos DB Emulator\PSModules\Microsoft.Azure.CosmosDB.Emulator"
$storageEmulatorExe = "${Env:ProgramFiles(x86)}\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" 

$cosmosEmulatorRunning = $false
$storageEmulatorRunning = $false

function IsStorageEmulatorRunning()
{
    $command = & $storageEmulatorExe "status"
    foreach ($line in $command)
    {
        if ($line.StartsWith("IsRunning: "))
        {
            if ($line.Replace("IsRunning: ", "") -eq "True")
            {                
                Write-Host "Storage emulator is already running."
                return $true
            }
        }      
    }
    return $false
}

Write-Host ""
Write-Host "---Starting CosmosDB emulator---"
$cosmosStatus = Get-CosmosDbEmulatorStatus

if ($cosmosStatus -ne "Running")
{
    Write-Host "CosmosDB emulator is not running. Starting emulator."
    Start-CosmosDbEmulator -NoWait
}
else
{
    $cosmosEmulatorRunning = $true
    Write-Host "CosmosDB emulator is already running."
}
Write-Host "------"
Write-Host ""
Write-Host "---Starting Storage emulator---"
$storageEmulatorRunning = IsStorageEmulatorRunning

if ($storageEmulatorRunning -eq $false)
{
    Write-Host "Storage emulator is not running. Starting emulator."    
    Start-Process -FilePath $storageEmulatorExe -ArgumentList "start"
}
Write-Host "------"
Write-Host 

if ($cosmosEmulatorRunning -eq $false)
{
    Write-Host "---Waiting for CosmosDB emulator to be running---"
    while ($cosmosStatus -ne "Running")
    {
        $cosmosStatus = Get-CosmosDbEmulatorStatus
        Start-Sleep -Seconds 2
    }
    Write-Host "Cosmos status: $cosmosStatus"
    Write-Host "------"
    Write-Host
}

if ($cosmosEmulatorRunning -eq $false)
{
    Write-Host "---Waiting for CosmosDB emulator to be running---"
    while ($cosmosStatus -ne "Running")
    {
        $cosmosStatus = Get-CosmosDbEmulatorStatus
        Start-Sleep -Seconds 2
    }
    Write-Host "Cosmos status: $cosmosStatus"
    Write-Host "------"
    Write-Host 
}

if ($storageEmulatorRunning -eq $false)
{
    Write-Host "---Waiting for Storage emulator to be running---"
    while (IsStorageEmulatorRunning -ne $true)
    {        
        Write-Host "Storage emulator not ready."
        Start-Sleep -Seconds 2
    }
    Write-Host Write-Host "Storage emulator ready."
    Write-Host "------"
    Write-Host     
}