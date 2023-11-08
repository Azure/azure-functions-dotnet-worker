## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk 1.16.0 (meta package)

- <entry>

### Microsoft.Azure.Functions.Worker.Sdk.Analyzers <version> (delete if not updated)

- <entry>

### Microsoft.Azure.Functions.Worker.Sdk.Generators <version>

- Reverted: Default to optimized function executor (#2012)
- Updated source generated versions of IFunctionExecutor to use `global::` namespace prefix to avoid build errors for function class with the same name as its containing namespace. (#1993)
- Updated source generated versions of IFunctionExecutor to include XML documentation for all public types and members
- Updated source generated versions of IFunctionMedatadaProvider to include XML documentation for all public types and members
