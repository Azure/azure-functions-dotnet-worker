[CmdletBinding()]
param(
    [switch] $TransportOnly,
    [switch] $RuntimeOnly,
    [switch] $RequireRuntimeLane
)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$artifacts = Join-Path $PSScriptRoot 'TestResults'
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

function Invoke-DotNetTest {
    param(
        [Parameter(Mandatory)] [string] $Project,
        [string] $Filter,
        [switch] $NoBuild
    )

    $arguments = @('test', $Project, '-c', 'Release', '--logger', "trx;LogFilePrefix=$([IO.Path]::GetFileNameWithoutExtension($Project))", '--results-directory', $artifacts)
    if ($Filter) {
        $arguments += @('--filter', $Filter)
    }
    if ($NoBuild) {
        $arguments += '--no-build'
    }

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet test failed for '$Project' with exit code $LASTEXITCODE."
    }
}

if (-not $RuntimeOnly) {
    Invoke-DotNetTest -Project (Join-Path $root 'test\Testing.Conformance\Testing.Conformance.csproj')
    $transportReport = [ordered]@{
        lane = 'transport'
        status = 'passed'
        unexplainedDifferences = 0
        normalizedFields = @('transport request IDs', 'transport timestamps')
        comparedFields = @(
            'message type and order',
            'function and invocation IDs',
            'status',
            'exception category and message',
            'payload bytes',
            'binding names',
            'trace attributes',
            'per-invocation log level, category, message, and order'
        )
    }
    $transportReport | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $artifacts 'transport-report.json') -Encoding utf8
}

if (-not $TransportOnly) {
    $repoCoreTools = Join-Path $root 'Azure.Functions.Cli'
    if (Test-Path (Join-Path $repoCoreTools $(if ($IsWindows) { 'func.exe' } else { 'func' }))) {
        $env:PATH = "$repoCoreTools$([IO.Path]::PathSeparator)$env:PATH"
    }

    $func = Get-Command func -ErrorAction SilentlyContinue
    $azurite = Get-Command azurite -ErrorAction SilentlyContinue
    $azuriteListening = $false
    $probe = [Net.Sockets.TcpClient]::new()
    try {
        $probe.Connect('127.0.0.1', 10000)
        $azuriteListening = $true
    }
    catch {
        $azuriteListening = $false
    }
    finally {
        $probe.Dispose()
    }
    $missing = @()
    if (-not $func) {
        $missing += 'Azure Functions Core Tools'
    }
    if (-not $azurite -and -not $azuriteListening) {
        $missing += 'Azurite'
    }

    if ($missing.Count -gt 0) {
        $runtimeReport = [ordered]@{
            lane = 'runtime-smoke'
            status = 'skipped-local-prerequisite'
            missing = $missing
        }
        $runtimeReport | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $artifacts 'runtime-smoke-report.json') -Encoding utf8
        if ($RequireRuntimeLane) {
            throw "The required runtime smoke lane is missing: $($missing -join ', ')."
        }
        Write-Host "Runtime smoke lane skipped locally: $($missing -join ', ')."
    }
    else {
        Invoke-DotNetTest `
            -Project (Join-Path $root 'test\DotNetWorker.Testing.Tests\DotNetWorker.Testing.Tests.csproj') `
            -Filter 'FullyQualifiedName~BuiltInHttpTests|FullyQualifiedName~Invocation_ExecutesRealPipelineAndReturnsIdValueAndLogs'
        Invoke-DotNetTest `
            -Project (Join-Path $root 'test\extensions\Worker.Extensions.Http.AspNetCore.Testing.Tests\Worker.Extensions.Http.AspNetCore.Testing.Tests.csproj')
        Invoke-DotNetTest `
            -Project (Join-Path $root 'test\E2ETests\E2ETests\E2ETests.csproj') `
            -Filter 'FullyQualifiedName~RuntimeSmoke_BuiltInHttp|FullyQualifiedName~QueueTriggerAndOutput_Succeeds' `
            -NoBuild
        Invoke-DotNetTest `
            -Project (Join-Path $root 'test\E2ETests\E2ETests\E2ETests.csproj') `
            -Filter 'FullyQualifiedName~RuntimeSmoke_AspNetCoreHttp' `
            -NoBuild

        $runtimeReport = [ordered]@{
            lane = 'runtime-smoke'
            status = 'passed'
            coveredBehaviors = @(
                'real built-in HTTP status and body',
                'real queue trigger and durable output',
                'real ASP.NET Core HTTP status, body, and user log'
            )
            note = 'This lane is independent real-host smoke coverage; it does not claim differential parity with the in-process factory.'
        }
        $runtimeReport | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $artifacts 'runtime-smoke-report.json') -Encoding utf8
    }
}
