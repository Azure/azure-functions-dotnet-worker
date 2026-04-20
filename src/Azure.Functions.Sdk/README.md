# Azure.Functions.Sdk

An [MSBuild SDK](https://learn.microsoft.com/dotnet/core/project-sdk/overview) for building Azure Functions .NET isolated worker applications. This SDK replaces `Microsoft.Azure.Functions.Worker.Sdk` and is imported via the `Sdk` attribute on the `<Project>` element rather than as a `PackageReference`.

## Getting Started

Set the `Sdk` attribute on your project's `<Project>` element:

```xml
<Project Sdk="Azure.Functions.Sdk/0.3.0">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore" Version="2.1.0" />
  </ItemGroup>

</Project>
```

The SDK automatically provides:

- `Microsoft.NET.Sdk.Worker` (the underlying worker SDK)
- `Microsoft.Azure.Functions.Worker` package reference
- `AzureFunctionsVersion` set to `v4`
- Source generators and analyzers for function metadata
- Azure Functions tooling integration (`dotnet run` launches the Functions host)

You only need to add your trigger and binding extension packages.

## Migrating from Microsoft.Azure.Functions.Worker.Sdk

Migration only requires changes to your project file (`.csproj`). **No C# code changes are needed.**

The old SDK used a `PackageReference` and required you to set several properties manually:

```xml
<!-- Before: Microsoft.Azure.Functions.Worker.Sdk -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <AzureFunctionsVersion>v4</AzureFunctionsVersion>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Azure.Functions.Worker" Version="2.51.0" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" Version="2.0.0" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore" Version="2.0.0" />
  </ItemGroup>

</Project>
```

To migrate, make the following project file changes:

1. **Change the `Sdk` attribute** from `Microsoft.NET.Sdk` to `Azure.Functions.Sdk/<version>`.
2. **Remove the `Microsoft.Azure.Functions.Worker.Sdk` package reference** — it is now the SDK itself.
3. **Remove the `Microsoft.Azure.Functions.Worker` package reference** — it is included implicitly by the SDK.
4. **Remove `OutputType`** — the worker SDK sets this automatically.
5. **Remove `AzureFunctionsVersion`** — the SDK defaults to `v4`.

```xml
<!-- After: Azure.Functions.Sdk -->
<Project Sdk="Azure.Functions.Sdk/0.3.0">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore" Version="2.0.0" />
  </ItemGroup>

</Project>
```

> **Note:** Both SDKs use the same analyzers, source generators, and runtime packages. Migration changes only the MSBuild targets that drive the build — your application code, `Program.cs`, function classes, and `host.json` all remain unchanged.

## Generated Extension Project (`azure_functions.g.csproj`)

During restore, the SDK generates a helper project named `azure_functions.g.csproj` in your `obj/` directory. This project is used to resolve the function extension assemblies required by the Azure Functions host. It is restored automatically and its outputs are included in your build and publish output.

You should **not** build or reference this project directly. If a traversal or `dirs.proj` file accidentally includes it, see [AZFW0109](https://github.com/Azure/azure-functions-dotnet-worker/blob/feature/msbuild-sdk/docs/sdk-rules/AZFW0109.md) for guidance.

## SDK Rules

The SDK emits diagnostics prefixed with `AZFW01xx`. See the full list of SDK rules [here](https://github.com/Azure/azure-functions-dotnet-worker/blob/feature/msbuild-sdk/docs/sdk-rules/index.md).
