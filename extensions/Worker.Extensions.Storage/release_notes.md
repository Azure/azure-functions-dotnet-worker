## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.Storage 6.8.3

- Update `Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs` to `6.8.3` (#3516)
- Update `Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues` to `5.5.6` (#3516)

### Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs 6.8.3

- Update `Microsoft.Azure.WebJobs.Extensions.Storage.Blobs` to `5.3.10` (#3516)
  - Fixes BlobTrigger polling regressions introduced in `5.3.8`: blobs uploaded after the first scan are now discovered when the listing continuation marker is empty, and blobs modified during a complete single-page listing are no longer skipped (Azure/azure-sdk-for-net#62764)

### Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues 5.5.6

- Update `Microsoft.Azure.WebJobs.Extensions.Storage.Queues` to `5.3.10` (#3516)