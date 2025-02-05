## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk <version>

- Addresses issue with `dotnet build --no-incremental` failing build (PR #2763, Issue #2601)
- Addresses issue with setting `OutDir` msbuild property failing build (PR #2763, Issue #2125)
- Adds support for externally supplied worker extensions project. (PR #2763, Issues #1252, #1888)
    - This mode is opt-in only. Using it eliminates the inner-build. This is useful for scenarios where performing a restore inside of build was problematic.

### Microsoft.Azure.Functions.Worker.Sdk.Generators 1.3.5

- Update all generated classes to use `GeneratedCodeAttribute`. (#2887)
