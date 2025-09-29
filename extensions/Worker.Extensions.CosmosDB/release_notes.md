## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.CosmosDB <version>

- Address an issue where `WorkerOptions.Serializer` was not respected for CosmosDB bindings. (#3163)
  - only bindings to Cosmos client, database, or container correctly used WorkerOptions.Serializer. Now all bindings will.
  - **CAUTION**: this may cause deserialization changes depending on your setup. To revert this behavior, configure the CosmosDB serializer manually.
