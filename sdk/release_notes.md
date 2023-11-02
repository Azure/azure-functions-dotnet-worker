## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk 1.16.0 (meta package)

- Default to source-generated function metadata (#1968).
    * If you have `<FunctionsEnableWorkerIndexing>True</FunctionsEnableWorkerIndexing>` in your `.csproj` file, you can remove that line after upgrading Azure.Functions.Worker.Sdk version 1.16.0 or later.