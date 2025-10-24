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

To use from another project, like a test project, add this target:

``` xml
<PropertyGroup>
  <!-- update this path as needed -->
  <ResolverProject>Azure.Functions.Sdk.Resolver/Azure.Functions.Sdk.Resolver.csproj</ResolverProject>
</PropertyGroup>

<ItemGroup>
  <ProjectReference Include="$(ResolverProject)" ReferenceOutputAssembly="false" />
</ItemGroup>

<Target Name="_AssignSdkFiles" BeforeTargets="AssignTargetPaths" AfterTargets="ResolveProjectReferences">
  <MSBuild Projects="$(ResolverProject)" Targets="GetSdkFiles">
    <Output TaskParameter="TargetOutputs" ItemName="_SdkFiles" />
  </MSBuild>

  <ItemGroup>
    <None Include="@(_SdkFiles)" TargetPath="resolver/%(_SdkFiles.TargetPath)" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Target>
```

The `<some-folder>` from above can then be `<Folder-Containing-Your-Test-Assembly>/resolver`
