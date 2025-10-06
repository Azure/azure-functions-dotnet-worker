#Requires -Version 6

param(
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [String]
    [ValidateSet("net7", "netfx")]
    $DotnetVersion,

    [Switch]
    $SkipStorageEmulator,

    [Switch]
    $SkipCosmosDBEmulator,

    [Switch]
    $SkipCoreTools,

    [Switch]
    $UseCoreToolsBuildFromIntegrationTests,

    [Switch]
    $SkipBuildOnPack
)

$FunctionsRuntimeVersion = 4

function StopOnFailedExecution {
  if ($LastExitCode)
  {
    exit $LastExitCode
  }
}

if($SkipCoreTools)
{
  Write-Host
  Write-Host "---Skipping Core Tools download---"
}
else
{
  $arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()

  if ($IsWindows) {
      $os = "win"
      $coreToolsURL = $env:CORE_TOOLS_URL
  }
  elseif ($IsMacOS) {
      $os = "osx"
  }
  else {
      $os = "linux"
      $coreToolsURL = $env:CORE_TOOLS_URL_LINUX
  }

  if ($UseCoreToolsBuildFromIntegrationTests -eq $true -or $UseCoreToolsBuildFromIntegrationTests.IsPresent) {
    Write-Host ""
    Write-Host "Using Core Tools from integration test feed..."

    if ([string]::IsNullOrWhiteSpace($coreToolsURL)) {
        Write-Error "CORE_TOOLS_URL (or CORE_TOOLS_URL_LINUX) is not set."
        exit 1
    }
  }
  else {
    Write-Host ""
    Write-Host "Using latest Core Tools release from GitHub..."

    # GitHub API call for latest release
    $releaseInfo = Invoke-RestMethod -Uri "https://api.github.com/repos/Azure/azure-functions-core-tools/releases/latest" -Headers @{ "User-Agent" = "PowerShell" }

    $latestVersion = $releaseInfo.tag_name
    Write-Host "`nLatest Core Tools version: $latestVersion"

    # Look for zip file matching os and arch
    $pattern = "Azure\.Functions\.Cli\.$os-$arch\..*\.zip$"
    $asset = $releaseInfo.assets | Where-Object {
        $_.name -match $pattern
    }

    if (-not $asset) {
        Write-Error "Could not find a Core Tools .zip for OS '$os' and arch '$arch'"
        exit 1
    }

    $coreToolsURL = $asset.browser_download_url
  }

  # Append query string to avoid caching issues
  $coreToolsURL = $coreToolsURL + "?raw=true";

  Write-Host ""
  Write-Host "---Downloading the Core Tools for Functions V$FunctionsRuntimeVersion---"
  Write-Host "Core Tools download URL: $coreToolsURL"

  $FUNC_CLI_DIRECTORY = Join-Path $PSScriptRoot 'Azure.Functions.Cli'
  Write-Host 'Deleting Functions Core Tools if exists...'
  Remove-Item -Force "$FUNC_CLI_DIRECTORY.zip" -ErrorAction Ignore
  Remove-Item -Recurse -Force $FUNC_CLI_DIRECTORY -ErrorAction Ignore

  $output = "$FUNC_CLI_DIRECTORY.zip"
  Invoke-RestMethod -Uri $coreToolsURL -OutFile $output

  Write-Host 'Extracting Functions Core Tools...'
  Expand-Archive $output -DestinationPath $FUNC_CLI_DIRECTORY

  if ($IsMacOS -or $IsLinux)
  {
    & "chmod" "a+x" "$FUNC_CLI_DIRECTORY/func"
  }

  Write-Host "------"
}

if (Test-Path $output)
{
  Remove-Item $output -Recurse -Force -ErrorAction Ignore
}

Write-Host "----- Executing tests for Dotnet version $DotnetVersion -----"

$AdditionalPackArgs = @("-c", "Release", "-p:FunctionsRuntimeVersion=$FunctionsRuntimeVersion", "-p:DotnetVersion=$DotnetVersion")

if ($SkipBuildOnPack -eq $true)
{
    $AdditionalPackArgs += "--no-build"
}

.\tools\devpack.ps1 -DotnetVersion $DotnetVersion -E2E -AdditionalPackArgs $AdditionalPackArgs

if ($SkipStorageEmulator -And $SkipCosmosDBEmulator)
{
  Write-Host
  Write-Host "---Skipping emulator startup---"
  Write-Host
}
else
{
  .\tools\start-emulators.ps1 -SkipStorageEmulator:$SkipStorageEmulator -SkipCosmosDBEmulator:$SkipCosmosDBEmulator
}

StopOnFailedExecution