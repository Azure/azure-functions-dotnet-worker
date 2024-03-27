## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore 1.3.0

- Improvements to context coordination/synchronization handling and observability
  - Failure to receive any of the expected context synchronization calls will now result in a `TimeoutException` thrown with the appropriate exception information. Previously this would block indefinitely and failures here were difficult to diagnose.
  - Debug logs are now emitted in the context coordination calls, improving observability.
- Introduces fix to properly handle multiple output binding scenarios (#2322).
