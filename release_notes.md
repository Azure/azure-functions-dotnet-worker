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
- When the host advertises the `HostJson` capability (value `1`) in the worker init request, the worker now returns the raw `host.json` bytes (up to 64 KiB) from `FUNCTIONS_APPLICATION_DIRECTORY` in `WorkerInitResponse.host_json`, with a structured status for missing, unreadable (including an unavailable or invalid app directory), or oversized cases. The snapshot is read once per process; restart the worker to retry a failed read. Without the capability, behavior is unchanged. (Azure/azure-functions-host#12049)