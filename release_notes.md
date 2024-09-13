## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker (metapackage) 2.0.0

- Updating `Microsoft.Azure.Functions.Worker.Core` to 2.0.0
- Updating `Microsoft.Azure.Functions.Worker.Grpc` to 2.0.0

### Microsoft.Azure.Functions.Worker.Core 2.0.0

- Capability `IncludeEmptyEntriesInMessagePayload` is now enabled by default (#2701)
  - This means that empty entries will be included in the function trigger message payload by default.
  - To disable this capability and return to the old behaviour, set `IncludeEmptyEntriesInMessagePayload` to `false` in the worker options.

    ```csharp
    var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
    }, options =>
    {
        options.IncludeEmptyEntriesInMessagePayload = false;
    })
    .Build();
    ```

### Microsoft.Azure.Functions.Worker.Grpc 2.0.0

- <entry>
