param(    
    [Parameter(Mandatory=$false)]
    [Switch]
    $SkipEmulators,
    [Parameter(Mandatory=$false)]
    [Switch]
    $SkipCoreTools
)

# A function that checks exit codes and fails script if an error is found 
function StopOnFailedExecution {
  if ($LastExitCode) 
  { 
    exit $LastExitCode 
  }
}

$currDir =  Get-Location
$output = "$currDir/Azure.Functions.Cli.zip"

if($SkipCoreTools)
{
  Write-Host
  Write-Host "---Skipping Core Tools download---"  
}
else
{
  Write-Host
  Write-Host "---Core Tools download---"
  Write-Host "Deleting Functions Core Tools if exists...."
  Remove-Item -Force ./Azure.Functions.Cli.zip -ErrorAction Ignore
  Remove-Item -Recurse -Force ./Azure.Functions.Cli -ErrorAction Ignore

  if (!$IsWindows -and !$IsLinux)
  {
    # For pre-PS6
    Write-Host "Could not resolve OS. Assuming Windows."
    $IsWindows = $true
  }
  
  $coreToolsURL = ""
  
  if ($IsWindows)
  {
    $coreToolsURL = $env:CORE_TOOLS_URL
    if (!$coreToolsURL)
    {        
        $coreToolsURL = "https://functionsclibuilds.blob.core.windows.net/builds/3/latest/Azure.Functions.Cli.min.win-x86.zip"
        Write-Host "Using default url for Core Tools Windows: $coreToolsURL"
        Invoke-RestMethod -Uri 'https://functionsclibuilds.blob.core.windows.net/builds/3/latest/version.txt' -OutFile version.txt
    }
  }
  elseif ($IsLinux)
  {
    $coreToolsURL = $env:CORE_TOOLS_URL_LINUX
    if (!$coreToolsURL)
    {       
        $coreToolsURL = "https://functionsclibuilds.blob.core.windows.net/builds/3/latest/Azure.Functions.Cli.linux-x64.zip"
        Write-Host "Using default url for Core Tools Linux: $coreToolsURL"
        Invoke-RestMethod -Uri 'https://functionsclibuilds.blob.core.windows.net/builds/3/latest/version.txt' -OutFile version.txt
    }
  }
  else
  {
      Write-Host "Could not determine the core tools URL."
      exit 1
  }
  Write-Host "Downloading Core Tools from $coreToolsURL"
  if (Test-Path version.txt)
  {
    Write-Host "Using Functions Core Tools version: $(Get-Content -Raw version.txt)"
    Remove-Item version.txt
  }  

  $wc = New-Object System.Net.WebClient
  $wc.DownloadFile($coreToolsURL, $output)

  $destinationPath = "./Azure.Functions.Cli"
  Write-Host "Extracting Functions Core Tools to $destinationPath"
  Expand-Archive "./Azure.Functions.Cli.zip" -DestinationPath $destinationPath
  
  if ($IsLinux)
  {
    & "chmod" "a+x" "$destinationPath/func"
  }
  
  Write-Host "------"
}

if (Test-Path $output) 
{
  Remove-Item $output
}

.\tools\devpack.ps1 -E2E -AdditionalPackArgs @("-c","Release") -SkipBuildOnPack

if (!$SkipEmulators)
{
  .\tools\start-emulators.ps1
}
else 
{
  Write-Host
  Write-Host "---Skipping emulator startup---"
  Write-Host
}

StopOnFailedExecution