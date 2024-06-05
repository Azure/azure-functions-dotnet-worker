## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.CosmosDB 5.0.0

- Implement `CosmosDBExtensionOptions` to allow configuration of the CosmosDB service client via `CosmosClientOptions` (#2483)

#### Breaking Change

- **Default `ConnectionMode` Change:** The default `ConnectionMode` for `CosmosClientOptions` has been changed from `Gateway` to `Direct`.
  This change is due to `Direct` being the default value for `ConnectionMode` in the Cosmos SDK; now that we are exposing those options,
  it is not possible for us to tell whether the user has explicitly set the connection mode to `Direct` or not, so we must default to the
  SDK's default value

##### How to Maintain Previous Behavior

To continue using `Gateway` mode, configure the `CosmosClientOptions` in your `Program` class as shown below:

```csharp
.ConfigureFunctionsWorkerDefaults((builder) =>
{
    builder.ConfigureCosmosDBExtensionOptions((options) =>
    {
        options.ClientOptions.ConnectionMode = ConnectionMode.Gateway;
    });
})
```
