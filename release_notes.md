## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker (metapackage) <version>

- Update `Microsoft.Azure.Functions.Worker.Core` to <version>
- Update `Microsoft.Azure.Functions.Worker.Grpc` to <version>

### Microsoft.Azure.Functions.Worker.Core <version>

- Add support for propagating trace context tags from worker to host (#3303)
- Add support for propagating OpenTelemetry Baggage to the worker. Requires use with the OpenTelemetry Extension to work end to end (#3319).

### Microsoft.Azure.Functions.Worker.Grpc <version>

- Update protobuf version to v1.12.0-protofile and add support for propagating tags from the worker to the functions host (#3303).
- Update protobuf version to v1.13.0-protofile to add support for propagating OpenTelemetry baggage to the worker (#3319).

### Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore <version>

- Add `Action<IApplicationBuilder>` overloads to `ConfigureFunctionsWebApplication` for `FunctionsApplicationBuilder` (#3052)