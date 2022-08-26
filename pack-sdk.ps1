$packageSuffix = "dev" + [datetime]::UtcNow.Ticks.ToString()
$outputDirectory = "C:\Users\likasem\source\buildoutput"
$project = "C:\Users\likasem\source\repos\azure-functions-dotnet-worker\sdk\Sdk\Sdk.csproj"

dotnet --version

dotnet build

$cmd = "pack", "$project", "-o", $outputDirectory, "--no-build", "--version-suffix", "-$packageSuffix"

& dotnet $cmd

# nuget init $outputDirectory $localNuget
Copy-Item -Path "C:\Users\likasem\source\buildoutput\*" -Destination "C:\Users\likasem\source\localnuget" -Recurse