## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker (metapackage) 2.0.0

- Updating `Microsoft.Azure.Functions.Worker.Core` to 2.0.0
- Updating `Microsoft.Azure.Functions.Worker.Grpc` to 2.0.0

### Microsoft.Azure.Functions.Worker.Core 2.0.0

- `ValidateScopes` is enabled for developement environments by default. (#2705)
  - The default is the value of `IsDevelopment(IHostingEnvironment)`.
- Capability `IncludeEmptyEntriesInMessagePayload` is now enabled by default (#2701)
  - This means that empty entries will be included in the function trigger message payload by default.
  - To disable this capability and return to the old behaviour, set `IncludeEmptyEntriesInMessagePayload` to `false` in the worker options.
- Capability `EnableUserCodeException` is now enabled by default (#2702)
  - This means that exceptions thrown by user code will be surfaced to the Host as their original exception type, instead of being wrapped in an RpcException.
  - To disable this capability and return to the old behaviour, set `EnableUserCodeException` to `false` in the worker options.
  - The `EnableUserCodeException` property in WorkerOptions has been marked as obsolete and may be removed in a future release.
- Rename `ILoggerExtensions` to `FunctionsLoggerExtensions` to avoid naming conflict issues (#2716)
- Updating `Azure.Core` to 1.41.0
- Updated service registrations for bootstrapping methods to ensure idempotency.

##### Setting worker options example

```csharp
var host = new HostBuilder()
.ConfigureFunctionsWorkerDefaults(options =>
{
    options.EnableUserCodeException = false;
    options.IncludeEmptyEntriesInMessagePayload = false;
})
```

### Microsoft.Azure.Functions.Worker.Grpc 2.0.0

- Removed fallback command line argument reading code for grpc worker startup options. (#1908)

### Microsoft.Azure.Functions.Worker.Sdk 2.0.0-preview2

- Adding support for SDK container builds with Functions base images
- Updating `Azure.Core` to 1.41.0
- Updated service registrations for bootstrapping methods to ensure idempotency.