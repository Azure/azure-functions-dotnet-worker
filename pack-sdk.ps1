$packageSuffix = "dev" + [datetime]::UtcNow.Ticks.ToString()
$outputDirectory = "/Users/likasem/source/buildoutput"
$project = "/Users/likasem/source/functions/azure-functions-dotnet-worker/sdk/Sdk/Sdk.csproj"

dotnet --version

dotnet build

$cmd = "pack", "$project", "-o", $outputDirectory, "--no-build", "--version-suffix", "-$packageSuffix"

& dotnet $cmd

# nuget init $outputDirectory $localNuget
Copy-Item -Path "/Users/likasem/source/buildoutput/*" -Destination "/Users/likasem/source/localnuget" -Recurse