## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk <version> (meta package)

- Default to source-generated function metadata (#1968)

### Microsoft.Azure.Functions.Worker.Sdk.Analyzers <version> (delete if not updated)

- <entry>

### Microsoft.Azure.Functions.Worker.Sdk.Generators <version>

- Default to source-generated function metadata (#1968)
  - If you had `<FunctionsEnableWorkerIndexing>True</FunctionsEnableWorkerIndexing>` in your `.csproj``, you can remove it after upgrading to this version.
- Updated source generated versions of FunctionExecutor to use `global::` namespace prefix to avoid build errors for function class with the same name as its containing namespace. (#1993)

