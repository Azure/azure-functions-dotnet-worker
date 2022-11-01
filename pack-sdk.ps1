# FOR TESTING - WILL DELETE

$packageSuffix = "dev" + [datetime]::UtcNow.Ticks.ToString()
$outputDirectory = "../../buildoutput"
$project = "sdk/Sdk/Sdk.csproj"

dotnet --version

dotnet build

$cmd = "pack", "$project", "-o", $outputDirectory, "--no-build", "--version-suffix", "-$packageSuffix"

& dotnet $cmd
