# Packs the SDK locally, updates the Sample to use this package, and then builds the sample.

$rootPath = Split-Path -Parent $PSScriptRoot

`dotnet pack $rootPath\sdk\sdk\Sdk.csproj -o $rootPath\local
`dotnet remove $rootPath\samples\FunctionApp\FunctionApp.csproj package Microsoft.Azure.Functions.Worker.Sdk
`dotnet add $rootPath\samples\FunctionApp\FunctionApp.csproj package Microsoft.Azure.Functions.Worker.Sdk -s $rootPath\local --prerelease
`dotnet build $rootPath\samples\FunctionApp\FunctionApp.csproj