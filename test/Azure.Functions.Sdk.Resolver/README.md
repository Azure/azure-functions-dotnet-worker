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
