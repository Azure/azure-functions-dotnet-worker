# FunctionsNetHost.SiteExtension

This project builds the site extension package for Azure Functions .NET Worker Host.

## Usage

Restore, build, and publish as a standard MSBuild project. To zip the output, use:

    dotnet publish -c Release -p:ZipAfterPublish=true

The zipped package will be found in the `out/pkg/{config}` directory.
