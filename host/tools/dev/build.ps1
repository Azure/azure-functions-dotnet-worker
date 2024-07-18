# Ensure the out directory exists, then clean its contents
$outDir = "..\..\out"
if (Test-Path -Path $outDir) {
    Remove-Item -Path "$outDir\*" -Recurse -Force
}

# Build and publish FunctionsNetHost
cd "src\FunctionsNetHost"

# Run dotnet build with release configuration and output to the relative out directory
dotnet publish "FunctionsNetHost.csproj" -c Release -o $outDir

# Navigate to PlaceholderApp directory
cd "..\..\src\PlaceholderApp\"

# Clean PlaceholderApp using dotnet clean
dotnet clean

dotnet restore

# Publish PlaceHolderApp to net9.0 folder.
$placeholderAppPathNet9 = Join-Path -Path "..\..\out\net9.0" -ChildPath "PlaceholderApp"
if (-not (Test-Path -Path $placeholderAppPathNet9)) {
    New-Item -ItemType Directory -Path $placeholderAppPathNet9
}

dotnet publish "PlaceholderApp.csproj" -f net9.0 -c Release -o $placeholderAppPathNet9

# Publish PlaceHolderApp to net8.0 folder with -f net8.0.
$placeholderAppPathNet8 = Join-Path -Path "..\..\out\net8.0" -ChildPath "PlaceholderApp"
if (-not (Test-Path -Path $placeholderAppPathNet8)) {
    New-Item -ItemType Directory -Path $placeholderAppPathNet8
}

dotnet publish "PlaceholderApp.csproj" -c Release -o $placeholderAppPathNet8 -f net8.0

cd "..\..\"

