# Azure.Functions.Sdk.Tests

Contains tests for [Azure.Functions.Sdk](/src/Azure.Functions.Sdk/).

This project also performs E2E/integration tests with MSbuild execution.

## Assembly Loads

Testing dotnet builds poses a unique challenge with assembly loads. We are using MSBuild APIs directly to invoke build & restore, which means all of msbuild and its tasks run under this test projects assembly load context. But we have no idea what the entire set of assemblies msbuild needs is, they are not part of our resolved references!

The solution is to use a custom assembly resolution hook which will try to load assemblies from the dotnet SDK installation location directly. This works, but be careful it is fragile! It only loads if we **do not** have that assembly ourselves. This means if this project brings in an assembly the SDK wants also, but incompatible versions, tests may fail with assembly load errors.

To consume a nuget package, but still let it be compatible with the SDK, there are two options:

1. Ensure the same version, or higher, of the package is referenced by this project
2. Add `ExcludeAssets="runtime"` to the `PackageReference` to allow compilation, but _not_ include the assembly in the final build output, allowing it to be loaded from the SDK directory.

> [!IMPORTANT]
> This custom assembly resolution from SDK directory is **not** available during xunit test discovery. This means we cannot have any types from assemblies that are resolved from the SDK directory exposed in any `public` type of this test project. If you see xunit test discovery fail with assembly load errors, this is most likely the cause.
