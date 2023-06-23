## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk <version> (meta package)

- Update extension build project to reference Microsoft.NET.Sdk.Functions 4.2.0
- Added retries on `IOException` when writing `function.metadata` file as part of `GenerateFunctionMetadata` msbuild task. This is to allow builds to continue (with warnings) when another process has the file momentarily locked. If the file continues to be locked the task (and build) will fail after 10 retries with a 1 second delay each. (#1532)
- Add support for deferred binding (#1676)

### Microsoft.Azure.Functions.Worker.Sdk.Analyzers <version> (delete if not updated)

- <entry>

### Microsoft.Azure.Functions.Worker.Sdk.Generators <version>

- Add support for retry options (#1548)
- Bug fix for when DefaultValue is not present on an IsBatched prop (#1602).
- SDK changes for .NET 8.0 support (#1643)
