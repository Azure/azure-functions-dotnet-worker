## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker (metapackage) <version>

- <entry>

### Microsoft.Azure.Functions.Worker.Core <version>

- Updating `Azure.Core` to 1.41.0
- Updated service registrations for bootstrapping methods to ensure idempotency.

### Microsoft.Azure.Functions.Worker.Grpc <version>

- Removed fallback command line argument reading code for grpc worker startup options. (#1908)
- Updating `Azure.Core` to 1.41.0
- Updated service registrations for bootstrapping methods to ensure idempotency.