## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk <version> (meta package)

- Update extension build project to reference Microsoft.NET.Sdk.Functions 4.2.0

### Microsoft.Azure.Functions.Worker.Sdk.Analyzers <version>

- Add analyzer for SupportsDeferredBindingAttribute #1367
- Added an analyzer that will show a warning for types not supported by a binding attribute (#1505)
- Added an analyzer that will suggest a code refactor for all of the types supported by a binding attribute (#1604)

### Microsoft.Azure.Functions.Worker.Sdk.Generators <version>

- Add support for retry options (#1548)
- Bug fix for when DefaultValue is not present on an IsBatched prop (#1602).
- SDK changes for .NET 8.0 support (#1643)
