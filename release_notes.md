### Release notes
<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker 1.14.0 (meta package)
- Update Microsoft.Azure.Functions.Worker.Core dependency to 1.12.0
- Update Microsoft.Azure.Functions.Worker.Grpc dependency to 1.10.0
### Microsoft.Azure.Functions.Worker.Core 1.12.0
- Fix `ArgumentOutOfRangeException` when using `HttpDataRequestDataExtensions.ReadAsStringAsync` in .NET Framework (#1466)
### Microsoft.Azure.Functions.Worker.Grpc 1.10.0
- Including worker metadata & capabilities in env reload response (#1425)
- Fix race condition causing GrpcWorker initialization failure (#1508)
- Fix `null` reference exception when retry context is not set (#1476)