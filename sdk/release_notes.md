## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk 2.1.0

- Emit a build warning directing users to the `Azure.Functions.Sdk` MSBuild SDK when targeting .NET 11 (#3435)

### Microsoft.Azure.Functions.Worker.Sdk.Generators 1.4.0

- Added compile-time duplicate function name detection (AZFW0017) (#3158)
- Always emit the generated function metadata provider and executor, even when an app declares zero functions or all declared functions fail validation (including duplicate function names), so apps stay on the generated provider instead of failing at indexing time (#3446)
