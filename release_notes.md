## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker (metapackage) <version>

- Update protobuf to v1.9.0-protofile, which includes gRPC messages for retry-options in worker-indexing scenarios. (#1545)

### Microsoft.Azure.Functions.Worker.Core <version>

- Fixed issue spawning child process while debugging due to "DOTNET_STARTUP_HOOKS" always containing "Microsoft.Azure.Functions.Worker.Core". (#1539)

### Microsoft.Azure.Functions.Worker.Grpc <version>

- <event>

### Microsoft.Azure.Functions.Worker.Sdk

- Added retries on `IOException` when writing `function.metadata` file as part of `GenerateFunctionMetadata` msbuild task. This is to allow builds to continue (with warnings) when another process has the file momentarily locked. If the file continues to be locked the task (and build) will fail after 10 retries with a 1 second delay each. (#1532)
