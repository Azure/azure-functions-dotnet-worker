## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker (metapackage) <version>

- <entry>

### Microsoft.Azure.Functions.Worker.Core <version>

- <entry>

### Microsoft.Azure.Functions.Worker.Grpc <version>

- Ensured gRPC worker background stream loops do not capture an ambient SynchronizationContext, preventing a worker hang when the application uses a single-threaded context (e.g. Nito.AsyncEx AsyncContext). (#3455)
- When the host requests it with the `HostConfiguration` capability (value `1`), the worker returns `host.json` converted to strict BOM-free UTF-8 (source UTF-8, or UTF-8/UTF-16/UTF-32 with a BOM; at most 65,536 converted bytes) from `FUNCTIONS_APPLICATION_DIRECTORY` in `WorkerInitResponse.host_configuration`, with a structured status for missing, error (unavailable or invalid app directory, unreadable or invalidly encoded file), or oversized cases. The worker does not parse the JSON. The snapshot is captured once per process on the first requested init; restart the worker to retry a failed read. Without the capability the worker omits the field and does not read `host.json`. (Azure/azure-functions-host#12049)