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
$sdkProject = "$rootPath/build/DotNetWorker.Core.slnf"
$httpProject = "$rootPath/extensions/Worker.Extensions.Http/src/Worker.Extensions.Http.csproj"
$abstractionsProject = "$rootPath/extensions/Worker.Extensions.Abstractions/src/Worker.Extensions.Abstractions.csproj"

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
Write-Host "Packing Core .NET Worker projects to $localPack"
& "dotnet" "pack" $sdkProject "-p:PackageOutputPath=$localPack" "-nologo" "-p:BuildNumber=$buildNumber" $AdditionalPackArgs
Write-Host

Write-Host
Write-Host "Packing HTTP extension projects to $localPack"
& "dotnet" "pack" $httpProject "-p:PackageOutputPath=$localPack" "-nologo" "-p:BuildNumber=$buildNumber" $AdditionalPackArgs
Write-Host

Write-Host
Write-Host "Packing extension abstraction projects to $localPack"
& "dotnet" "pack" $abstractionsProject "-p:PackageOutputPath=$localPack" "-nologo" "-p:BuildNumber=$buildNumber" $AdditionalPackArgs
Write-Host

