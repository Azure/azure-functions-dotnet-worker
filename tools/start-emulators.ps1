param(
    [Parameter(Mandatory=$false)]
    [Switch]
    $SkipStorageEmulator,
    [Parameter(Mandatory=$false)]
    [Switch]
    $SkipCosmosDBEmulator,
    [Parameter(Mandatory=$false)]
    [Switch]
    $NoWait
)

$DebugPreference = 'Continue'

Write-Host "Skip CosmosDB Emulator: $SkipCosmosDBEmulator"
Write-Host "Skip Storage Emulator: $SkipStorageEmulator"

$startedCosmos = $false
$startedStorage = $false

if (!$IsWindows -and !$IsLinux -and !$IsMacOs)
{
    # For pre-PS6
    Write-Host "Could not resolve OS. Assuming Windows."
    $IsWindows = $true
}

if (!$IsWindows)
{
    Write-Host "Skipping CosmosDB emulator because it is not supported on non-Windows OS."
    $SkipCosmosDBEmulator = $true
}

if (!$SkipCosmosDBEmulator)
{
    # Locally, you may need to run PowerShell with administrative privileges
    Add-MpPreference -ExclusionPath "$env:ProgramFiles\Azure Cosmos DB Emulator"
    Import-Module "$env:ProgramFiles\Azure Cosmos DB Emulator\PSModules\Microsoft.Azure.CosmosDB.Emulator"
}

function IsStorageEmulatorRunning()
{
    try
    {
        $response = Invoke-WebRequest -Uri "http://127.0.0.1:10000/"
        $StatusCode = $Response.StatusCode
    }
    catch
    {
        $StatusCode = $_.Exception.Response.StatusCode.value__
    }

    if ($StatusCode -eq 400)
    {
        return $true
    }

    return $false
}

if (!$SkipCosmosDBEmulator)
{
    Write-Host ""
    Write-Host "---Starting CosmosDB emulator---"
    $cosmosStatus = Get-CosmosDbEmulatorStatus
    Write-Host "CosmosDB emulator status: $cosmosStatus"

    if ($cosmosStatus -eq "StartPending")
    {
        $startedCosmos = $true
    }
    elseif ($cosmosStatus -ne "Running")
    {
        Write-Host "CosmosDB emulator is not running. Starting emulator."
        Start-Process "$env:ProgramFiles\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe" "/NoExplorer /NoUI /DisableRateLimiting /PartitionCount=50 /Consistency=Strong /EnablePreview /EnableSqlComputeEndpoint" -Verb RunAs
        $startedCosmos = $true
    }
    else
    {
        Write-Host "CosmosDB emulator is already running."
    }
}

if (!$SkipStorageEmulator)
{
    Write-Host "------"
    Write-Host ""
    Write-Host "---Starting Storage emulator---"
    $storageEmulatorRunning = IsStorageEmulatorRunning

    if ($storageEmulatorRunning -eq $false)
    {
        if ($IsWindows)
        {
            npm install -g azurite
            Start-Process azurite.cmd -ArgumentList "--silent"
        }
        else
        {
            sudo npm install -g azurite
            sudo mkdir azurite
            sudo azurite --silent --location azurite --debug azurite\debug.log &
        }

        $startedStorage = $true
    }
    else
    {
        Write-Host "Storage emulator is already running."
    }

    Write-Host "------"
    Write-Host
}

if ($NoWait -eq $true)
{
    Write-Host "'NoWait' specified. Exiting."
    Write-Host
    exit 0
}

if (!$SkipCosmosDBEmulator -and $startedCosmos -eq $true)
{
    Write-Host "---Waiting for CosmosDB emulator to be running---"
    $cosmosStatus = Get-CosmosDbEmulatorStatus
    Write-Host "CosmosDB emulator status: $cosmosStatus"

    $waitSuccess = Wait-CosmosDbEmulator -Status Running -Timeout 60 -ErrorAction Continue

    if ($waitSuccess -ne $true)
    {
        Write-Host "CosmosDB emulator not yet running after waiting 60 seconds. Restarting."
        Write-Host "Shutting down and restarting"
        Start-Process "$env:ProgramFiles\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe" "/Shutdown" -Verb RunAs
        sleep 30;

        for ($j=0; $j -lt 3; $j++) {
            Write-Host "Attempt $j"
            Start-Process "$env:ProgramFiles\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe" "/NoExplorer /NoUI /DisableRateLimiting /PartitionCount=50 /Consistency=Strong /EnablePreview /EnableSqlComputeEndpoint" -Verb RunAs

            for ($i=0; $i -lt 5; $i++) {
                $status = Get-CosmosDbEmulatorStatus
                Write-Host "Cosmos DB Emulator Status: $status"

                if ($status -ne "Running") {
                    sleep 30;
                }
                else {
                    break;
                }
            }

            if ($status -ne "Running") {
                Write-Host "Shutting down and restarting"
                Start-Process "$env:ProgramFiles\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe" "/Shutdown" -Verb RunAs
                sleep 30;
            }
            else {
                break;
            }
        }

        if ($status -ne "Running") {
            Write-Error "Emulator failed to start"
        }
    }

    Write-Host "------"
    Write-Host
}

if (!$SkipStorageEmulator -and $startedStorage -eq $true)
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