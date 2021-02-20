param(
    [Parameter(Mandatory=$false)]
    [Switch]
    $E2E
)

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
& "dotnet" "pack" "$rootPath\sdk\sdk\Sdk.csproj" "-o" "$localPack" "-nologo"
Write-Host
Write-Host "Updating SDK package reference in $project"
& "dotnet" "remove" $project "package" "Microsoft.Azure.Functions.Worker.Sdk"
& "dotnet" "add" $project "package" "Microsoft.Azure.Functions.Worker.Sdk" "-s" "$localPack" "--prerelease"
Write-Host
Write-Host "Building $project"
& "dotnet" "build" $project "-nologo"
Write-Host "------"
