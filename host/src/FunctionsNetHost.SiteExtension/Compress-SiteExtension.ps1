param(
    [string[]]$JitTrace
)

$publishDir = Join-Path $PSScriptRoot '..\..\..\..\out\pub\FunctionsNetHost.SiteExtension\Release_win'
$zipPath = Join-Path $PSScriptRoot '..\..\..\..\out\pkg\Release\FunctionsNetHost.SiteExtension.zip'

Write-Host "Compressing site extension from $publishDir to $zipPath"
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -Force

if ($JitTrace) {
    Write-Host "Inserting JIT trace files: $JitTrace"
    foreach ($trace in $JitTrace) {
        Copy-Item $trace -Destination $publishDir -Force
    }
}
