param(
    [ValidateSet('x64','x86')][string]$Bitness = 'x64',
    [switch]$NoZip
)

$publishDir = Join-Path $PSScriptRoot "..\..\..\..\out\pub\FunctionsNetHost.SiteExtension\Release_win"
$privateDir = Join-Path $PSScriptRoot "..\..\..\..\out\private\FunctionsNetHost.SiteExtension.$Bitness"

Write-Host "Creating private site extension at $privateDir"
Copy-Item $publishDir $privateDir -Recurse -Force

if (-not $NoZip) {
    $zipPath = "$privateDir.zip"
    Write-Host "Zipping private site extension to $zipPath"
    Compress-Archive -Path "$privateDir\*" -DestinationPath $zipPath -Force
}
