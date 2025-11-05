# Azure.Functions.Sdk.Resolver

This project scaffolds out a custom `Microsoft.Build.Framework.SdkResolver` which will automatically resolve the `Azure.Functions.Sdk/99.99.99` SDK for integration testing.

To use this, you must scaffold out the contents as follows:

```
- <some-folder>
  |- Azure.Functions.Sdk.Resolver
    |- Azure.Functions.Sdk.Resolver.dll
    |- sdk/*
    |- targets/*
    |- build/*
    |- tools/*
```

You then set the environment variable `MSBUILDADDITIONALSDKRESOLVERSFOLDER=<some-folder>`.

## Ways to use

### 1. From Another Project

To use from another project, like a test project, simply reference the project. Consider adding `ReferenceOutputAssembly="false"` as the dll is not directly needed.

``` xml
<ItemGroup>
  <ProjectReference Include="Azure.Functions.Sdk.Resolver/Azure.Functions.Sdk.Resolver.csproj" ReferenceOutputAssembly="false" />
</ItemGroup>
```

The resolver will be scaffolded out to `$(OutDir)/resolver`. Use the resolver by setting the appropriate environment variable:

``` csharp
string resolverPath = Path.Combine(Path.GetDirectoryName(typeof(SomeTypeInTestAssembly).Assembly.Location)!, "resolver");
Environment.SetEnvironmentVariable("MSBUILDADDITIONALSDKRESOLVERSFOLDER", resolverPath); // Set this before evaluating your project via MSBuild APIs.
```

### 2. Locally

To use this resolver locally (useful for inner dev loop debugging):

1. Publish the project: `dotnet publish -o {path-you-want}`. The output path is optional.
2. Run `init.ps1` from the published location. This will set the env variable for MSBuild to discover the resolver.
   1. This sets the env var for the current terminal only.
3. Use `Sdk="Azure.Functions.Sdk/99.99.99"` from a csproj. Must be version `99.99.99`.
4. Restore/build/publish via `dotnet` in the same environment init.ps1 was run.
