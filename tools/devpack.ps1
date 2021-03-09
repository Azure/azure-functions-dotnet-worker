param(
    [Parameter(Mandatory=$false)]
    [Switch]
    $E2E,
    [Parameter(Mandatory=$false)]
    [Switch]
    $SkipBuildOnPack,
    [Parameter(Mandatory=$false)]
    [string[]]
    $AdditionalPackArgs = @()
)

# Packs the SDK locally, and (by default) updates the Sample to use this package, then builds.
# Specify --E2E to instead target the E2E test app.

$buildNumber = "local" + [System.DateTime]::Now.ToString("yyyyMMddHHmm")

Write-Host
Write-Host "Building packages with BuildNumber $buildNumber"

$rootPath = Split-Path -Parent $PSScriptRoot
$project = "$rootPath/samples/FunctionApp/FunctionApp.csproj"
$sdkProject = "$rootPath/build/DotNetWorker.Core.slnf"

if($E2E -eq $true)
{
    $project = "$rootPath/test/E2ETests/E2EApps/E2EApp/E2EApp.csproj"
}

if ($SkipBuildOnPack -eq $true)
{
  $AdditionalPackArgs +="--no-build";  
}

$localPack = "$rootPath/local"
if (!(Test-Path $localPack))
{
  New-Item -Path $localPack -ItemType directory | Out-Null
}
Write-Host
Write-Host "---Updating project with local SDK pack---"
Write-Host "Packing Core .NET Worker projects to $localPack"
& "dotnet" "pack" $sdkProject "-o" "$localPack" "-nologo" "-p:BuildNumber=$buildNumber" $AdditionalPackArgs
Write-Host

Write-Host "Removing SDK package reference in $project"
& "dotnet" "remove" $project "package" "Microsoft.Azure.Functions.Worker.Sdk"
Write-Host

Write-Host "Removing Worker package reference in $project"
& "dotnet" "remove" $project "package" "Microsoft.Azure.Functions.Worker"
Write-Host

Write-Host "Finding latest local Worker package in $localPack"
$package = Find-Package Microsoft.Azure.Functions.Worker -Source $localPack
$version = $package.Version
Write-Host "Found $version"
Write-Host

Write-Host "Adding Worker package version $version to $project"
& "dotnet" "add" $project "package" "Microsoft.Azure.Functions.Worker" "-v" $version "-s" $localPack "-n"
Write-Host

Write-Host "Finding latest local SDK package in $localPack"
$package = Find-Package "Microsoft.Azure.Functions.Worker.Sdk" -Source $localPack
$version = $package.Version
Write-Host "Found $version"
Write-Host
Write-Host "Adding SDK package version $version to $project"
& "dotnet" "add" $project "package" "Microsoft.Azure.Functions.Worker.Sdk" "-v" $version "-s" $localPack "-n"
Write-Host
$configFile = Split-Path "$project"
$configFile += "/NuGet.Config"
Write-Host "Config file name" $configFile
& "dotnet" "nuget" "add" "source" $localPack "--name" "local" "--configfile" "$configFile"
Write-Host "Building $project"

& "dotnet" "build" $project "-nologo" "-p:TestBuild=true"
Write-Host "------"
