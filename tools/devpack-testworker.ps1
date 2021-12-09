param(
    [Parameter(Mandatory=$false)]
    [Switch]
    $SkipBuildOnPack,
    [Parameter(Mandatory=$false)]
    [string[]]
    $AdditionalPackArgs = @()
)

# Packs the TestWorker locally, and (by default) updates the TestWorker sample to use this package, then builds.
$buildNumber = "local" + [System.DateTime]::Now.ToString("yyyyMMddHHmm")

Write-Host
Write-Host "Building packages with BuildNumber $buildNumber"

$rootPath = Split-Path -Parent $PSScriptRoot
$project = "$rootPath/samples/TestProject/TestProject.csproj"
$projectReference = "$rootPath/src/DotNetWorker.TestWorker/DotNetWorker.TestWorker.csproj"
$packageName = "Microsoft.Azure.Functions.Worker.TestWorker"
$sdkProject = "$rootPath/src/DotNetWorker.TestWorker/DotNetWorker.TestWorker.csproj"

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
Write-Host "---Updating project with local package---"
Write-Host "Packing $sdkProject to $localPack"
& "dotnet" "pack" $sdkProject "-o" "$localPack" "-nologo" "-p:BuildNumber=$buildNumber" $AdditionalPackArgs
Write-Host

Write-Host "Removing reference to $projectReference in $project"
& "dotnet" "remove" $project "reference" "$projectReference"
Write-Host

Write-Host "Finding latest local Worker package in $localPack"
$package = Find-Package $packageName -Source $localPack
$version = $package.Version
Write-Host "Found $version"
Write-Host

Write-Host "Adding package version $version to $project"
& "dotnet" "add" $project "package" "$packageName" "-v" $version "-s" $localPack
Write-Host