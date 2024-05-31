## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk 1.17.3

- Removing redefining of msbuild target `_FunctionsPreBuild` (#2498)
- SDK changes for .NET 9.0 support (#2497)

### Microsoft.Azure.Functions.Worker.Sdk.Generators 1.3.1

- ExtensionStartupRunnerGenerator generating code which conflicts with customer code (namespace) (#2542)
- Enhanced function metadata generation to include `$return` binding for HTTP trigger functions. (#1619)
