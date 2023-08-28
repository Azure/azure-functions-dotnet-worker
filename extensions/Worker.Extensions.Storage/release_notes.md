## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.Storage 6.1.1

- Updated `Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs` to 6.1.1

### Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs 6.1.1

- Implement IFunctionsWorkerApplicationBuilder.ConfigureBlobStorage() extension method
  - F# projects need to configure the extension manually due to source gen restrictions
