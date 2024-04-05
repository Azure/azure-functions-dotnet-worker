## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk 1.18.0

- Updating to use `Microsoft.NET.Sdk.Functions.Generators` 1.2.2 (#2247)

### Microsoft.Azure.Functions.Worker.Sdk.Generators 1.3.0

- Introduces handling for `HttpResultAttribute`, which is used on HTTP response properties in [multiple output-binding scenarios](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide?tabs=windows#multiple-output-bindings). Example:

```csharp
public class MyOutputType
{
    [QueueOutput("myQueue")]
    public string Name { get; set; }

    [HttpResult]
    public IActionResult HttpResponse { get; set; }
}
```
