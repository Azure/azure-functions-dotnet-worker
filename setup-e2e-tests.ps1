
# A function that checks exit codes and fails script if an error is found 
function StopOnFailedExecution {
  if ($LastExitCode) 
  { 
    exit $LastExitCode 
  }
}

Write-Host
Write-Host "---Core Tools download---"

$skipCliDownload = $false
if($args[0])
{
  $skipCliDownload = $args[0]
  Write-Host "Skipping CLI Download"
}

$currDir =  Get-Location
$output = "$currDir\Azure.Functions.Cli.zip"
if(!$skipCliDownload)
{
  Write-Host "Deleting Functions Core Tools if exists...."
  Remove-Item -Force ./Azure.Functions.Cli.zip -ErrorAction Ignore
  Remove-Item -Recurse -Force ./Azure.Functions.Cli -ErrorAction Ignore

  if (-not (Test-Path env:CORE_TOOLS_URL)) 
  { 
    $env:CORE_TOOLS_URL = "https://functionsclibuilds.blob.core.windows.net/builds/3/latest/Azure.Functions.Cli.win-x86.zip"
  }

  Write-Host "Downloading Functions Core Tools to $output"
  Invoke-RestMethod -Uri 'https://functionsclibuilds.blob.core.windows.net/builds/3/latest/version.txt' -OutFile version.txt
  Write-Host "Using Functions Core Tools version: $(Get-Content -Raw version.txt)"
  Remove-Item version.txt
    
  $wc = New-Object System.Net.WebClient
  $wc.DownloadFile($env:CORE_TOOLS_URL, $output)

  $destinationPath = ".\Azure.Functions.Cli"
  Write-Host "Extracting Functions Core Tools to $destinationPath"
  Expand-Archive ".\Azure.Functions.Cli.zip" -DestinationPath $destinationPath
}

if (Test-Path $output) 
{
  Remove-Item $output
}

Write-Host "------"

.\tools\devpack.ps1 -E2E

.\tools\start-emulators.ps1

StopOnFailedExecution