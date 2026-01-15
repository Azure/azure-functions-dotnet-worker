param(
    [Parameter(Mandatory=$false)]
    [switch][Alias("r")]$Release
)

function Find-Root
{
    $current = Get-Item -Path $PSScriptRoot

    while ($null -ne $current)
    {
        if (Test-Path -Path (Join-Path -Path $current.FullName -ChildPath ".reporoot"))
        {
            return $current.FullName
        }

        $current = $current.Parent
    }

    throw "Could not find root directory containing .reporoot"
}

$root = Find-Root
$resolver = Join-Path -Path $root -ChildPath "test/Azure.Functions.Sdk.Resolver/Azure.Functions.Sdk.Resolver.csproj"

$command = "dotnet publish $resolver"
$config = "-c debug"
if ($Release)
{
    $config = "-c release"
}

$command += " $config"

# Use IEX to split '$config' into separate switch and arg.
$publishDir = Invoke-Expression "dotnet build $resolver $config -getproperty:PublishDir"
$publishDir = $publishDir.Trim()

# Make path absolute if relative
if (-not [System.IO.Path]::IsPathRooted($publishDir)) {
    $publishDir = [System.IO.Path]::GetFullPath($publishDir, [System.IO.Path]::Combine($root, "test", "Azure.Functions.Sdk.Resolver"))
}

Write-Host "Publishing Azure.Functions.Sdk for local development. Reference via 'Azure.Functions.Sdk/99.99.99' in your project file."
Write-Host "Can be only referenced from this environment session."
Write-Host "To use in another environment, set the MSBUILDADDITIONALSDKRESOLVERSFOLDER environment variable to '$publishDir'."

$env:MSBUILDADDITIONALSDKRESOLVERSFOLDER = $publishDir.Trim()
Invoke-Expression $command
