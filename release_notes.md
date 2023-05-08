## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker 1.14.1 (meta package)
- Update Microsoft.Azure.Functions.Worker.Core dependency to 1.12.1
- Update Microsoft.Azure.Functions.Worker.Grpc dependency to 1.10.1
### Microsoft.Azure.Functions.Worker.Core 1.12.1
- Minor documentation updates (no functional changes)
### Microsoft.Azure.Functions.Worker.Grpc 1.10.1
- Fixed an issue causing throughput degradation and for synchronous functions, blocked the execution pipeline. (#1516)
