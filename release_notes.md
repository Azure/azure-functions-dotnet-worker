## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker (metapackage) 2.50.0-<version>

- Adding `net10.0` TFM support.

### Microsoft.Azure.Functions.Worker.Core 2.50.0-<version>

- Support for function metadata transforms (#3145)
- Support setting `IFunctionExecutor` in invocation features: `FunctionContext.Features` (#3200)

### Microsoft.Azure.Functions.Worker.Grpc 2.50.0-<version>

- Updated to use the new metadata manage and leverage metadata transforms (#3145)
