## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker (metapackage) <version>

- <entry>

### Microsoft.Azure.Functions.Worker.Core <version>

- <entry>

### Microsoft.Azure.Functions.Worker.Grpc <version>

- <entry>

### Microsoft.Azure.Functions.Worker.Sdk 1.13.0-preview1

- Improve incremental build support for worker extension project inner build [#1749](https://github.com/Azure/azure-functions-dotnet-worker/pull/1749)
    - Now builds to intermediate output path
    - Avoids disk writes for generated .csproj and file copies if nothing has changed
