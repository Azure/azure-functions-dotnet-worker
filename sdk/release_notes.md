## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk 1.17.3-preview1 (meta package)

- Re-add SDK refactors that were reverted in #2313 (#2347)
- Address assembly scanning regression (#2347)
- Updating to use `Microsoft.NET.Sdk.Functions.Generators` 1.3.0 (#2322)
- Update legacy generator to handle `HttpResultAttribute` (#2342), which is used on HTTP response properties in [multiple output-binding scenarios](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide?tabs=windows#multiple-output-bindings). Example:

```csharp
public class MyOutputType
{
    [QueueOutput("myQueue")]
    public string Name { get; set; }

    [HttpResult]
    public IActionResult HttpResponse { get; set; }
}
```

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
