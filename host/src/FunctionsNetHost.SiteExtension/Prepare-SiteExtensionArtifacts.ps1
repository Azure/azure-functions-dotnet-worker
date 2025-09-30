# Prepare SiteExtension Artifacts for Azure Functions .NET Worker Host

$repoRoot = Resolve-Path "$PSScriptRoot\..\..\..\.."
$workerOutput = "$repoRoot\artifacts\dotnet-isolated"

# 1. Build FunctionsNetHost for Windows
Write-Host "Publishing FunctionsNetHost for win-x64..."
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $workerOutput
New-Item -ItemType Directory -Force -Path $workerOutput | Out-Null
$publishResult = dotnet publish "$repoRoot\azure-functions-dotnet-worker\host\src\FunctionsNetHost\FunctionsNetHost.csproj" -c Release -r win-x64 -o $workerOutput
Write-Host "dotnet publish output:"
$publishResult
if (!(Test-Path $workerOutput)) {
    Write-Error "Publish failed or output directory not created: $workerOutput"
    exit 1
}
if ((Get-ChildItem -Path $workerOutput | Measure-Object).Count -eq 0) {
    Write-Error "Publish succeeded but no files were produced in $workerOutput. Check project settings and runtime identifier."
    exit 1
}

Write-Host "dotnet-isolated worker output prepared at $workerOutput"