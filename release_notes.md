## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker (metapackage) 2.53.0-preview.1

- Update `Microsoft.Azure.Functions.Worker.Core` to 2.53.0-preview.1.
- Update `Microsoft.Azure.Functions.Worker.Grpc` to 2.53.0-preview.1.

### Microsoft.Azure.Functions.Worker.Core 2.53.0-preview.1

- Added the extension payload capability for compute-separated hosts. (#3525)
- Application directory resolution is cached on first use, preferring a nonempty, non-whitespace `FUNCTIONS_APPLICATION_DIRECTORY` and falling back to `AppContext.BaseDirectory`. Host configuration delivery, metadata indexing, extension payloads, function assembly loading, and ASP.NET Core endpoint discovery use this policy. Function assembly loading no longer falls back to `FUNCTIONS_WORKER_DIRECTORY`. (#3532)
- Added transport-independent host configuration acquisition and caching, registered with the core worker services. The snapshot reads `host.json` from the resolved application directory on first access, requires UTF-8 (an optional UTF-8 BOM is removed), and normalizes comments and trailing commas to strict compact JSON using System.Text.Json without reflection-based serialization, preserving values while potentially changing string escapes. Both the input excluding its BOM and the normalized payload are limited to 65,536 bytes. UTF-16/UTF-32 files must be saved as UTF-8; malformed UTF-8 and unsupported BOMs report the fixed diagnostic reason `InvalidUtf8`. Snapshots expose read-only UTF-8 bytes and structured statuses without transport dependencies. Failures are cached until process restart and log one warning with a fixed reason code, never paths or file content. (#3522, Azure/azure-functions-host#12049)

### Microsoft.Azure.Functions.Worker.Grpc 2.53.0-preview.1

- Use `FUNCTIONS_APPLICATION_DIRECTORY` for function assembly loading, falling back to `AppContext.BaseDirectory` instead of `FUNCTIONS_WORKER_DIRECTORY`. (#3532)
- Added on-demand extension payload streaming for compute-separated hosts. (#3525)
- Retry the initial host connection before opening the worker stream. (#3528)
- Ensured gRPC worker background stream loops do not capture an ambient SynchronizationContext, preventing a worker hang when the application uses a single-threaded context (e.g. Nito.AsyncEx AsyncContext). (#3455)
- When the host requests it with the `HostConfiguration` capability (value `1`), the gRPC worker obtains the Core host configuration snapshot and maps its status and content into `WorkerInitResponse.host_configuration`. Without the capability it omits the field and does not request or initialize the snapshot. Configuration failures are reported independently of the worker initialization result. (#3522, Azure/azure-functions-host#12049)
