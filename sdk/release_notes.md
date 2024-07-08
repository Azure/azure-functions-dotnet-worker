## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk <version> (meta package)

- Removing redefining of msbuild target `_FunctionsPreBuild` (#2498)

### Microsoft.Azure.Functions.Worker.Sdk.Generators <version>

- ExtensionStartupRunnerGenerator generating code which conflicts with customer code (namespace) (#2542)
- Updating generators to fix the namespace conflict with customer code (#2582)