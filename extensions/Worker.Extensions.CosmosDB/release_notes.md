## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.CosmosDB 4.9.0

- Implement `CosmosDBExtensionOptions` to allow configuration of the CosmosDB service client via `CosmosClientOptions` (#2483)

#### Example Usage

```csharp
.ConfigureFunctionsWorkerDefaults((builder) =>
{
    builder.ConfigureCosmosDBExtensionOptions((options) =>
    {
        options.ClientOptions.ConnectionMode = ConnectionMode.Direct;
        options.ClientOptions.ApplicationName = "MyApp";
    });
})
```
