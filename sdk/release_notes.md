## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk 1.16.0 (meta package)

- <entry>

### Microsoft.Azure.Functions.Worker.Sdk.Analyzers <version> (delete if not updated)

- <entry>

### Microsoft.Azure.Functions.Worker.Sdk.Generators <version>

- Updated source generated versions of FunctionExecutor to use `global::` namespace prefix to avoid build errors for function class with the same name as its containing namespace. (#1993)
- Updated source generated versions of FunctionExecutor to use case-sensitive comparison to fix incorrect invocation of functions with method names only differ in casing. (#2003)

