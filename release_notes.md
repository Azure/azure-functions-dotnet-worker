### Release notes
<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

- Source-generated function metadata: implementation change to improve cold-start performance (#956)

Steps for opting into the preview:

1. Add MSBuild property `<FunctionsEnableWorkerIndexing>true</FunctionsEnableWorkerIndexing>` app's `.csproj` file.
2. In `local.settings.json` add the property `"AzureWebJobsFeatureFlags": "EnableWorkerIndexing"` to configure the Azure Functions host to use worker-indexing.
3. Call the `IHostBuilder` extension, `ConfigureGeneratedFunctionMetadataProvider` in `Program.cs`:

```
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureGeneratedFunctionMetadataProvider()
                .Build();

            await host.RunAsync();
        }
    }
```
