## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk 2.0.7

- Reversal of change to suppress generation of functions.metadata file
- Updated the generated `WorkerExtensions` project to target `net10.0` instead of `net8.0` (#3444)

### Microsoft.Azure.Functions.Worker.Sdk.Generators <version>

- Added compile-time duplicate function name detection (AZFW0017) (#3158)
- Always emit the generated function metadata provider and executor, even when an app declares zero functions or all declared functions fail validation (including duplicate function names), so apps stay on the generated provider instead of failing at indexing time (#3446)