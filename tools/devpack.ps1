param(
    [Parameter(Mandatory=$false)]
    [Switch]
    $E2E
)


function Get-LatestPackage-Version([string] $packageName, [string] $packagePath)
{
    $packageVersion = $null

    Get-ChildItem "$packagePath" -Name |
    Foreach-Object {
        if ($_ -Match "$packageName")
        {
            # Converts the nupkg file name to a version
            $currentVersion = $_ -replace "$packageName.", ""
            $currentVersion = $currentVersion -replace ".nupkg", ""

            # Gets the highest version
            if ($currentVersion -gt $packageVersion)
            {
                $packageVersion = $currentVersion
            }
        }
    }

    if ($null -eq $packageVersion)
    {
        Throw "Did not find any package versions for '$packageName' in '$packagePath'"
    }

    return $packageVersion
}

# Packs the SDK locally, and (by default) updates the Sample to use this package, then builds.
# Specify --E2E to instead target the E2E test app.

$rootPath = Split-Path -Parent $PSScriptRoot
$project = "$rootPath\samples\FunctionApp\FunctionApp.csproj"

if($E2E -eq $true)
{
    $project = "$rootPath\test\E2ETests\E2EApps\E2EApp\E2EApp.csproj"
}

$localPack = "$rootPath\local"
Write-Host
Write-Host "---Updating project with local SDK pack---"
Write-Host "Packing SDK to $localPack"
& "dotnet" "pack" "$rootPath\sdk\sdk\Sdk.csproj" "-v" "q" "-o" "$localPack" "-nologo"
Write-Host
Write-Host "Updating SDK package reference in $project"
& "dotnet" "remove" $project "package" "Microsoft.Azure.Functions.Worker.Sdk"
$newestPackage = Get-LatestPackage-Version "Microsoft.Azure.Functions.Worker.Sdk" $localPack
& "dotnet" "add" $project "package" "Microsoft.Azure.Functions.Worker.Sdk" "-s" "$localPack" "-v" "$newestPackage"
Write-Host
Write-Host "Building $project"
& "dotnet" "build" $project "-v" "q" "-nologo"
Write-Host "------"
