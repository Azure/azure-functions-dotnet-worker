## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->


### Microsoft.Azure.Functions.Worker.Sdk 1.17.0-preview1 (meta package)

- Improve incremental build support for worker extension project inner build (https://github.com/Azure/azure-functions-dotnet-worker/pull/1749) 
  - Now builds to intermediate output path
- Resolve and pass nuget restore sources as explicit property to inner build (https://github.com/Azure/azure-functions-dotnet-worker/pull/1937)
- Integrate inner build with existing .NET SDK targets (https://github.com/Azure/azure-functions-dotnet-worker/pull/1861)
  - Targets have been refactored to participate with `CopyToOutputDirectory` and `CopyToPublishDirectory` instead of manually copying
  - Incremental build support further improved
- Explicitly error out if inner-builds TFM is altered. (https://github.com/Azure/azure-functions-dotnet-worker/pull/2222)
