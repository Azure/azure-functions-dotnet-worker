# Function Apps with the new MSBuild SDK

This sample shows a function app using the new MSBuild SDK. This SDK is a successor to `Microsoft.Azure.Functions.Worker.Sdk`.

Instead of being included via `PackageReference`, this new sdk uses the `Sdk` element.

1. Add pre-release nuget feed:

``` diff
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
+   <add key="pre-release" value="https://pkgs.dev.azure.com/azfunc/public/_packaging/pre-release/nuget/v3/index.json" />
  </packageSources>
</configuration>
```

2. Use the new SDK.

Minimal getting-started project:
``` xml
<Project Sdk="Azure.Functions.Sdk/[VERSION]">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>

    <!-- below are optional -->
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

This will automatically include the base package-references needed for the dotnet isolated worker runtime.

## What is different?

The biggest difference with this new SDK is how the extension bundle is created. AKA the 'inner-build'. In the old SDK, this was done entirely in the build phase. In this SDK, it is split between restore & build phase. However, this comes with some drawbacks:

1. Extension trimming no longer occurs. In the old SDK we would trim unused extensions from the bundle. This no longer occurs. Any referenced extension will be included, regardless of usage.
2. The new SDK relies on running targets post-restore. Post-restore hooks are not universal at the time of this doc authoring. See [here](/docs/sdk-rules/AZFW0108.md) for necessary steps to ensure post-restore hook runs.

## What is the same?

Your code! No changes to function app code -- only to the csproj.

## Publish/Deploy

ZipDeploy has not been added yet. To deploy a dotnet app using this SDK, follow these steps:

1. `dotnet publish -c release`
2. Zip up the published contents (zip the contents, not the folder itself)
3. `az functionapp deployment source config-zip -g <resource-group> -n <function-app-name> --src <zip>`
