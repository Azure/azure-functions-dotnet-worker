## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker (metapackage) <version>

- <entry>

### Microsoft.Azure.Functions.Worker.Core <version>

- Application directory resolution prefers a nonempty, non-whitespace `FUNCTIONS_APPLICATION_DIRECTORY`, falling back to `AppContext.BaseDirectory`. Host configuration delivery, metadata indexing, extension payloads, function assembly loading, and ASP.NET Core endpoint discovery use this policy. Function assembly loading no longer falls back to `FUNCTIONS_WORKER_DIRECTORY`.

- Added transport-independent host configuration acquisition and caching, registered with the core worker services. The snapshot reads `host.json` from the resolved application directory on first access, requires UTF-8 (an optional UTF-8 BOM is removed), and normalizes comments and trailing commas to strict compact JSON using System.Text.Json without reflection-based serialization, preserving values while potentially changing string escapes. Both the input excluding its BOM and the normalized payload are limited to 65,536 bytes. UTF-16/UTF-32 files must be saved as UTF-8; malformed UTF-8 and unsupported BOMs report the fixed diagnostic reason `InvalidUtf8`. Snapshots expose read-only UTF-8 bytes and structured statuses without transport dependencies. Failures are cached until process restart and log one warning with a fixed reason code, never paths or file content. (Azure/azure-functions-host#12049)

### Microsoft.Azure.Functions.Worker.Grpc <version>

- Added on-demand extension payload streaming for compute-separated hosts. (#3525)
- Ensured gRPC worker background stream loops do not capture an ambient SynchronizationContext, preventing a worker hang when the application uses a single-threaded context (e.g. Nito.AsyncEx AsyncContext). (#3455)
- When the host requests it with the `HostConfiguration` capability (value `1`), the gRPC worker obtains the Core host configuration snapshot and maps its status and content into `WorkerInitResponse.host_configuration`. Without the capability it omits the field and does not request or initialize the snapshot. Configuration failures are reported independently of the worker initialization result. (Azure/azure-functions-host#12049)
