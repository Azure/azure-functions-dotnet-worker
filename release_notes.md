## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker (metapackage) <version>

- Update protobuf to v1.9.0-protofile, which includes gRPC messages for retry-options in worker-indexing scenarios. (#1545)
- Add support for deferred binding (#1676)

### Microsoft.Azure.Functions.Worker.Core <version>

- Fixed issue spawning child process while debugging due to "DOTNET_STARTUP_HOOKS" always containing "Microsoft.Azure.Functions.Worker.Core". (#1539)
- Add retry options support to `IFunctionMetadata` (#1548)

### Microsoft.Azure.Functions.Worker.Grpc <version>

- Add handling for retry options in worker-indexing grpc communication path (#1548)
