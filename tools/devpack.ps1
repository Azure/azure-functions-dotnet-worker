param(
    [Parameter(Mandatory=$false)]
    [String]
    [ValidateSet("net8", "netfx")]
    $DotnetVersion,
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
$projects = @("$rootPath/samples/FunctionApp/FunctionApp.csproj")
$sdkProject = "$rootPath/build/DotNetWorker.Core.slnf"

if($E2E -eq $true)
{
    # Only add E2EAspNetCoreApp if DotnetVersion is not netfx
    if ($DotnetVersion -ne "netfx") {
        $projects += "$rootPath/test/E2ETests/E2EApps/E2EAspNetCoreApp/E2EAspNetCoreApp.csproj"
    }
    # Always add E2EApp project for E2E tests
    $projects += "$rootPath/test/E2ETests/E2EApps/E2EApp/E2EApp.csproj"
}

if ($SkipBuildOnPack -eq $true)
{
  $AdditionalPackArgs += "--no-build"
}

$localPack = "$rootPath/local"
if (!(Test-Path $localPack))
{
  New-Item -Path $localPack -ItemType directory | Out-Null
}
Write-Host
Write-Host "---Updating projects with local SDK pack---"
Write-Host "Packing Core .NET Worker projects to $localPack"
& "dotnet" "pack" $sdkProject "-p:PackageOutputPath=$localPack" "-nologo" "-p:Version=2.0.1" "-p:VersionSuffix=$buildNumber" $AdditionalPackArgs
Write-Host

foreach ($project in $projects) {
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
}
