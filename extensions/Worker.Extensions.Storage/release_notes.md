## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.Storage 6.2.0

- Updated `Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs` to 6.2.0

### Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs 6.2.0

- Updated `Microsoft.Azure.WebJobs.Extensions.Storage.Blobs` to 6.2.0
  - Added support for BlobsOptions.PoisonBlobThreshold
  - Bug fix: Updating ParameterBindingData "Connection" value to the full connection name instead of the connection section key
- Implement IFunctionsWorkerApplicationBuilder.ConfigureBlobStorage() extension method
  - F# projects need to configure the extension manually due to source gen restrictions
