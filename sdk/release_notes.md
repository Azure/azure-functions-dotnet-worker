## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk <version> (meta package)

- <entry>

### Microsoft.Azure.Functions.Worker.Sdk.Analyzers <version> (delete if not updated)

- <entry>

### Microsoft.Azure.Functions.Worker.Sdk.Generators <version>

- Updated source generated versions of FunctionExecutor to use `global::` namespace prefix to avoid ambiguity between namespaces.(#1847)
- Fixing the check to determine the function return type for async functions.(#1817)