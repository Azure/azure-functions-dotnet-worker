## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->


### Microsoft.Azure.Functions.Worker.Sdk 1.16.0-preview2

- Improve incremental build support for worker extension project inner build (https://github.com/Azure/azure-functions-dotnet-worker/pull/1749) 
  - Now builds to intermediate output path
- Resolve and pass nuget restore sources as explicit property to inner build (https://github.com/Azure/azure-functions-dotnet-worker/pull/1937)
- Integrate inner build with existing .NET SDK targets (https://github.com/Azure/azure-functions-dotnet-worker/pull/1861)
  - Targets have been refactored to participate with `CopyToOutputDirectory` and `CopyToPublishDirectory` instead of manually copying
  - Incremental build support further improved

### Microsoft.Azure.Functions.Worker.Sdk.Analyzers <version> (delete if not updated)

- <entry>

### Microsoft.Azure.Functions.Worker.Sdk.Generators <version>

- <entry>

