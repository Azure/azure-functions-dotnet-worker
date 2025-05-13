## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk 2.1.0-preview.1

- Addresses issue with `dotnet build --no-incremental` failing build (PR #2763, Issue #2601)
- Addresses issue with setting `OutDir` msbuild property failing build (PR #2763, Issue #2125)
- Write `worker.config` and `functions.metadata` only if contents have changed. (#2999)
- Updates the generated extension csproj to be net8.0 (from net6.0) 
- Setting _ToolingSuffix for V10.0 TargetFrameworkVersion. (#2983)
- Enhanced error message for missing Azure Functions Core Tools in the user's environment. (#2976)

### Microsoft.Azure.Functions.Worker.Sdk.Generators 1.3.5

- Update all generated classes to use `GeneratedCodeAttribute`. (#2887)
