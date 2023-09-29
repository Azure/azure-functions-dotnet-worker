## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->


### Microsoft.Azure.Functions.Worker.Sdk 1.15.0-preview1

- Improve incremental build support for worker extension project inner build [#1749](https://github.com/Azure/azure-functions-dotnet-worker/pull/1749) 
    - Now builds to intermediate output path - adding support for `nuget.config`
- Integrate inner build with existing .NET SDK targets [##1861](https://github.com/Azure/azure-functions-dotnet-worker/pull/1861)
    - Targets have been refactored to participate with `CopyToOutputDirectory` and `CopyToPublishDirectory` instead of manually copying
    - Incremental build support further improved

### Microsoft.Azure.Functions.Worker.Sdk.Analyzers <version> (delete if not updated)

- <entry>

### Microsoft.Azure.Functions.Worker.Sdk.Generators <version>

- <entry>