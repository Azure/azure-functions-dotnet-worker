## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk 2.0.6

- Build no longer generates `functions.metadata` if source-generated metadata provider is enabled. (#2974)
- Fixing `dotnet run` to work on Windows when core tools is installed from NPM (#3127)
- `Microsoft.Azure.Functions.Worker.Sdk.Generators` bumped to `1.3.6`.

### Microsoft.Azure.Functions.Worker.Sdk.Generators 1.3.6

- Fix bug that results in duplicate properties recorded for a binding (#3227)
