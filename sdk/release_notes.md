## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk 1.16.3 (meta package)

- Update worker.config generation to accurate worker executable name (#1053)
- Default to optimized function executor.
- Update Microsoft.Azure.Functions.Worker.Sdk.Generators dependency to 1.1.5

### Microsoft.Azure.Functions.Worker.Sdk.Generators 1.1.5

- Adding support for executing functions from referenced assemblies in the optimized function executor (#2089)
- Update worker.config generation to accurate worker executable name (#1053)
- Fix incorrect value of `ScriptFile` property in function metadata for .Net Framework function apps (#2103)
- Generate valid namespace when root namespace contains `-` (#2097)
- Bug fix for scenarios with `$return` output binding and `HttpTrigger` breaking output-binding rules (#2098)