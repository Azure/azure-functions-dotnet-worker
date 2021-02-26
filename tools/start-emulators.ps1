param(
    [Parameter(Mandatory=$false)]
    [Switch]
    $NoWait
)

Import-Module "$env:ProgramFiles\Azure Cosmos DB Emulator\PSModules\Microsoft.Azure.CosmosDB.Emulator"
$storageEmulatorExe = "${Env:ProgramFiles(x86)}\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" 

$startedCosmos = $false
$startedStorage = $false

function IsStorageEmulatorRunning()
{
    $command = & $storageEmulatorExe "status"
    foreach ($line in $command)
    {
        if ($line.StartsWith("IsRunning: "))
        {
            if ($line.Replace("IsRunning: ", "") -eq "True")
            {                
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
    $startedCosmos = $true
}
else
{    
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
    $startedStorage = $true
}
else
{
    Write-Host "Storage emulator is already running."
}
Write-Host "------"
Write-Host 

if ($NoWait -eq $true)
{
    Write-Host "'NoWait' specified. Exiting."
    Write-Host
    exit 0
}

if ($startedCosmos -eq $true)
{
    Write-Host "---Waiting for CosmosDB emulator to be running---"
    while ($cosmosStatus -ne "Running")
    {
        Write-Host "Cosmos emulator not ready. Status: $cosmosStatus"
        $cosmosStatus = Get-CosmosDbEmulatorStatus
        Start-Sleep -Seconds 5
    }
    Write-Host "Cosmos status: $cosmosStatus"
    Write-Host "------"
    Write-Host
}

if ($startedStorage -eq $true)
{
    Write-Host "---Waiting for Storage emulator to be running---"
    $storageEmulatorRunning = IsStorageEmulatorRunning
    while ($storageEmulatorRunning -eq $false)
    {        
        Write-Host "Storage emulator not ready."
        Start-Sleep -Seconds 5
        $storageEmulatorRunning = IsStorageEmulatorRunning
    }
    Write-Host "Storage emulator ready."
    Write-Host "------"
    Write-Host
}