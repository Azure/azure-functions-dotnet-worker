## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker (metapackage) 2.1.0

- `AZURE_FUNCTIONS_` environment variables are now loaded correctly when using `FunctionsApplicationBuilder`. (#2878)

### Microsoft.Azure.Functions.Worker.Core 2.1.0

- Support for function metadata transforms (#3145)

### Microsoft.Azure.Functions.Worker.Grpc 2.1.0

- Updated to use the new metadata manage and leverage metadata transforms (#3145)
