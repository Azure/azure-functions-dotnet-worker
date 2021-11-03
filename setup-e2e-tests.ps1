param(
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [String]
    [ValidateSet("3", "4")]
    $FunctionsRuntimeVersion,

    [Switch]
    $SkipStorageEmulator,

    [Switch]
    $SkipCosmosDBEmulator,

    [Switch]
    $SkipCoreTools,

    [Switch]
    $UseCoreToolsBuildFromIntegrationTests
)

# A function that checks exit codes and fails script if an error is found 
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
  else {
      if ($IsMacOS) {
          $os = "osx"
      } else {
          $os = "linux"
          $coreToolsURL = $env:CORE_TOOLS_URL_LINUX
      }
  }

  if ($UseCoreToolsBuildFromIntegrationTests.IsPresent)
  {
    Write-Host ""
    Write-Host "Install the Core Tools for Integration Tests..."
    $coreToolsURL = "https://functionsintegclibuilds.blob.core.windows.net/builds/$FunctionsRuntimeVersion/latest/Azure.Functions.Cli.$os-$arch.zip"
    $versionUrl = "https://functionsintegclibuilds.blob.core.windows.net/builds/$FunctionsRuntimeVersion/latest/version.txt"
  }
  else
  {
    if ([string]::IsNullOrWhiteSpace($coreToolsURL))
    {
      $coreToolsURL = "https://functionsclibuilds.blob.core.windows.net/builds/$FunctionsRuntimeVersion/latest/Azure.Functions.Cli.$os-$arch.zip"
      $versionUrl = "https://functionsclibuilds.blob.core.windows.net/builds/$FunctionsRuntimeVersion/latest/version.txt"
    }
  }

  Write-Host ""
  Write-Host "---Downloading the Core Tools for Functions V$FunctionsRuntimeVersion---"
  Write-Host "Core Tools download url: $coreToolsURL"

  $FUNC_CLI_DIRECTORY = Join-Path $PSScriptRoot 'Azure.Functions.Cli'
  Write-Host 'Deleting Functions Core Tools if exists...'
  Remove-Item -Force "$FUNC_CLI_DIRECTORY.zip" -ErrorAction Ignore
  Remove-Item -Recurse -Force $FUNC_CLI_DIRECTORY -ErrorAction Ignore

  if ($versionUrl)
  {
    $version = Invoke-RestMethod -Uri $versionUrl
    Write-Host "Downloading Functions Core Tools (Version: $version)..."
  }

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

.\tools\devpack.ps1 -E2E -AdditionalPackArgs @("-c","Release") -SkipBuildOnPack

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