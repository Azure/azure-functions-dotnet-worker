## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker (metapackage) 1.24.0

- Updating `Microsoft.Azure.Functions.Worker.Core` to 1.20.0
- Updating `Microsoft.Azure.Functions.Worker.Grpc` to 1.18.0

### Microsoft.Azure.Functions.Worker.Core 1.20.0

- Updated service registrations for bootstrapping methods to ensure idempotency. (#2820)

### Microsoft.Azure.Functions.Worker.Grpc 1.18.0

- Changed exception handling in function invocation path to ensure fatal exceptions bubble up. (#2789)
- Updated service registrations for bootstrapping methods to ensure idempotency. (#2820)