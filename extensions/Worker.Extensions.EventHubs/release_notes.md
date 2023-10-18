## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.EventHubs 6.0.1

- Updated Microsoft.Azure.WebJobs.Extensions.EventHubs dependency to 6.0.1
  - 6.0.0 introduces a breaking change: The default batch size has changed to 100 events. Previously, the default batch size was 10.
  - 6.0.1 fixes a bug by added support for the legacy checkpoint format when making scaling decisions
  - Read more about changes in the WebJobs extension [changelog](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventhub/Microsoft.Azure.WebJobs.Extensions.EventHubs/CHANGELOG.md)
