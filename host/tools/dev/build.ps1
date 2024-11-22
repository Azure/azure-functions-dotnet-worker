# You can use this script to build the FunctionsNetHost artifacts for local testing.
# This script should be executed from the "azure-functions-dotnet-worker\host" directory.

# Define paths
$outDir = "..\..\out"
# For local testing, feel free to update outDir path value as needed.
$outDir = "C:\Dev\OSS\azure-functions-host\out\bin\WebJobs.Script.WebHost\debugplaceholder\workers\dotnet-isolated\bin"
$functionsNetHostDir = "src\FunctionsNetHost"
$placeholderAppDir = "src\PlaceholderApp"
$placeholderAppPathNet9 = Join-Path -Path "$outDir\PlaceholderApp\9.0" -ChildPath "."
$placeholderAppPathNet8 = Join-Path -Path "$outDir\PlaceholderApp\8.0" -ChildPath "."

# Ensure the out directory exists, then clean its contents
if (Test-Path -Path $outDir) {
    Remove-Item -Path "$outDir\*" -Recurse -Force
}

# Build and publish FunctionsNetHost
cd $functionsNetHostDir
dotnet publish "FunctionsNetHost.csproj" -c Release -o $outDir

# Navigate to PlaceholderApp directory
cd "..\..\$placeholderAppDir"

# Clean and restore PlaceholderApp
dotnet clean
dotnet restore

# Function to publish PlaceholderApp
function Publish-PlaceholderApp {
    param (
        [string]$framework,
        [string]$outputPath
    )

    if (-not (Test-Path -Path $outputPath)) {
        New-Item -ItemType Directory -Path $outputPath
    }

    dotnet publish "FunctionsNetHost.PlaceholderApp.csproj" -f $framework -c Release -o $outputPath
}

# Publish PlaceholderApp to net9.0 and net8.0 folders
Publish-PlaceholderApp -framework "net9.0" -outputPath $placeholderAppPathNet9
Publish-PlaceholderApp -framework "net8.0" -outputPath $placeholderAppPathNet8

# Return to the root directory
cd "..\.."
