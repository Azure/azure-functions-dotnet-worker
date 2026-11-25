<#
.SYNOPSIS
    Reports WebJobs extension references in worker extension projects and compares them with NuGet.

.DESCRIPTION
    Scans extensions/*/src/*.csproj for <WebJobsExtension> items and, for each one, queries NuGet for the
    latest listed stable and prerelease versions of the WebJobs package. It also reports whether the worker
    package's current VersionPrefix has already been published, which determines whether a version bump is needed.

    NuGet is queried through https://www.nuget.org/api/v2/package-versions (listed versions only), falling back
    to the api.nuget.org flat container. The script fails if NuGet cannot be reached; it never guesses.

.PARAMETER Filter
    Optional wildcard filter matched against the WebJobs package id or the worker project name (for example '*Storage*').

.PARAMETER TargetVersion
    Optional version you intend to update to. Requires a filter that matches exactly one WebJobs package.
    Exits with code 2 when it is not the latest listed stable version on NuGet, and 1 when it is not listed at all.

.EXAMPLE
    ./Get-WebJobsExtensionStatus.ps1
    ./Get-WebJobsExtensionStatus.ps1 -Filter '*Storage.Blobs' -TargetVersion 5.3.10
#>
[CmdletBinding()]
param(
    [string] $Filter = '*',
    [string] $TargetVersion,
    [switch] $AsJson
)

$ErrorActionPreference = 'Stop'
$repoRoot = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
$versionCache = @{}

function Get-NuGetVersions([string] $packageId) {
    $key = $packageId.ToLowerInvariant()
    if ($versionCache.ContainsKey($key)) { return $versionCache[$key] }

    $urls = @(
        "https://www.nuget.org/api/v2/package-versions/$($packageId)?includePrerelease=true&semVerLevel=2.0.0",
        "https://api.nuget.org/v3-flatcontainer/$key/index.json"
    )
    $errors = @()
    foreach ($url in $urls) {
        try {
            $result = Invoke-RestMethod -Uri $url -TimeoutSec 30
            $versions = if ($result -is [array]) { $result } elseif ($result.versions) { $result.versions } else { @($result) }
            $versionCache[$key] = @($versions | Where-Object { $_ })
            return $versionCache[$key]
        }
        catch {
            $errors += "$url -> $($_.Exception.Message)"
        }
    }
    throw "Unable to query NuGet for '$packageId':`n$($errors -join "`n")"
}

function ConvertTo-VersionKey([string] $version) {
    $core, $pre = $version.Split('+')[0].Split('-', 2)
    $parts = @($core.Split('.') | ForEach-Object { [long] $_ })
    while ($parts.Count -lt 4) { $parts += 0 }
    [pscustomobject] @{ Parts = $parts; Pre = $pre }
}

function Compare-NuGetVersion([string] $a, [string] $b) {
    $x = ConvertTo-VersionKey $a
    $y = ConvertTo-VersionKey $b
    for ($i = 0; $i -lt 4; $i++) {
        if ($x.Parts[$i] -ne $y.Parts[$i]) { return [Math]::Sign($x.Parts[$i] - $y.Parts[$i]) }
    }
    if (-not $x.Pre -and -not $y.Pre) { return 0 }
    if (-not $x.Pre) { return 1 }
    if (-not $y.Pre) { return -1 }

    $xs = $x.Pre.Split('.')
    $ys = $y.Pre.Split('.')
    for ($i = 0; $i -lt [Math]::Min($xs.Count, $ys.Count); $i++) {
        $xn = 0L; $yn = 0L
        $xIsNum = [long]::TryParse($xs[$i], [ref] $xn)
        $yIsNum = [long]::TryParse($ys[$i], [ref] $yn)
        $c = if ($xIsNum -and $yIsNum) { [Math]::Sign($xn - $yn) }
             elseif ($xIsNum) { -1 }
             elseif ($yIsNum) { 1 }
             else { [Math]::Sign([string]::CompareOrdinal($xs[$i].ToLowerInvariant(), $ys[$i].ToLowerInvariant())) }
        if ($c -ne 0) { return $c }
    }
    return [Math]::Sign($xs.Count - $ys.Count)
}

function Get-Latest([string[]] $versions, [bool] $prerelease) {
    $latest = $null
    foreach ($v in @($versions | Where-Object { $prerelease -eq $_.Contains('-') })) {
        if (-not $latest -or (Compare-NuGetVersion $v $latest) -gt 0) { $latest = $v }
    }
    $latest
}

$projects = Get-ChildItem -Path (Join-Path $repoRoot 'extensions') -Filter *.csproj -Recurse
    | Where-Object { $_.Directory.Name -eq 'src' }

$results = foreach ($project in $projects) {
    [xml] $xml = Get-Content -Raw $project.FullName
    $references = @($xml.Project.ItemGroup.WebJobsExtension | Where-Object { $_ })
    foreach ($reference in $references) {
        $packageId = $reference.Include
        if ($packageId -notlike $Filter -and $project.BaseName -notlike $Filter) { continue }

        $workerPackageId = @($xml.Project.PropertyGroup.AssemblyName | Where-Object { $_ })[0]
        $workerVersion = @($xml.Project.PropertyGroup.VersionPrefix | Where-Object { $_ })[0]
        $versions = Get-NuGetVersions $packageId
        $workerVersions = Get-NuGetVersions $workerPackageId
        $latestStable = Get-Latest $versions $false
        $latestPrerelease = Get-Latest $versions $true
        if ($latestPrerelease -and $latestStable -and (Compare-NuGetVersion $latestPrerelease $latestStable) -lt 0) { $latestPrerelease = $null }

        [pscustomobject] @{
            WebJobsPackage         = $packageId
            CurrentVersion         = $reference.Version
            LatestStable           = $latestStable
            LatestPrerelease       = $latestPrerelease
            IsCurrentLatestStable  = $reference.Version -eq $latestStable
            WorkerProject          = [IO.Path]::GetRelativePath($repoRoot, $project.FullName)
            WorkerPackage          = $workerPackageId
            WorkerVersionPrefix    = $workerVersion
            WorkerVersionPublished = [bool] ($workerVersions -contains $workerVersion)
            LatestPublishedWorker  = Get-Latest $workerVersions $false
        }
    }
}

if ($AsJson) { $results | ConvertTo-Json -Depth 3 } else { $results | Format-List | Out-Host }

if ($TargetVersion) {
    $matched = @($results)
    if ($matched.Count -ne 1) {
        Write-Error "-TargetVersion requires -Filter to match exactly one WebJobs package; matched $($matched.Count)." -ErrorAction Continue
        exit 1
    }
    $item = $matched[0]
    if ((Get-NuGetVersions $item.WebJobsPackage) -notcontains $TargetVersion) {
        Write-Error "$($item.WebJobsPackage) $TargetVersion is not a listed version on NuGet." -ErrorAction Continue
        exit 1
    }
    if ($TargetVersion -ne $item.LatestStable) {
        Write-Warning "$($item.WebJobsPackage) $TargetVersion is NOT the latest listed stable version on NuGet ($($item.LatestStable)). Confirm this is intentional before continuing."
        exit 2
    }
    Write-Host "$($item.WebJobsPackage) $TargetVersion is the latest listed stable version on NuGet."
}
