### Release notes

<!-- Please add your release notes in the following format:
- My change description (#PR)
-->
- Using hostMetadataProvider for CreateOrUpdate call in FunctionController (#10678)
- Suppress `JwtBearerHandler` logs from customer logs (#10617)
- Address issue with HTTP proxying throwing `ArgumentException` (#10616)
- Updated JobHost restart suppresion in functions APIs to align with request lifecycle (#10638)
- Update Python Worker Version to [4.34.0](https://github.com/Azure/azure-functions-python-worker/releases/tag/4.34.0)
- Sanitize exception logs (#10443)
- Improving console log handling during specialization (#10345)
- Update Node.js Worker Version to [3.10.1](https://github.com/Azure/azure-functions-nodejs-worker/releases/tag/v3.10.1)
- Remove packages `Microsoft.Azure.Cosmos.Table` and `Microsoft.Azure.DocumentDB.Core` (#10503)
- Buffering startup logs and forwarding to ApplicationInsights/OpenTelemetry after logger providers are added to the logging system (#10530)
- Implement host configuration property to allow configuration of the metadata provider timeout period (#10526)
  - The value can be set via `metadataProviderTimeout` in host.json and defaults to "00:00:30" (30 seconds).
  - For logic apps, unless configured via the host.json, the timeout is disabled by default.
- Update PowerShell 7.2 worker to [4.0.4025](https://github.com/Azure/azure-functions-powershell-worker/releases/tag/v4.0.4025)
- Update PowerShell 7.4 worker to [4.0.4026](https://github.com/Azure/azure-functions-powershell-worker/releases/tag/v4.0.4026)
- Added support for identity-based connections to Diagnostic Events (#10438)
- Updating Microsoft.Azure.WebJobs.Logging.ApplicationInsights to 3.0.42-12121
- Updated retry logic in Worker HTTP proxy to allow for longer worker HTTP listener initialization times (#10566).
- Introduced proper handling in environments where .NET in-proc is not supported.
- Updated System.Memory.Data reference to 8.0.1
