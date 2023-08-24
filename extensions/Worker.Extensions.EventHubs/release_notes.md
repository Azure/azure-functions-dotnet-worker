## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.EventHubs 5.6.0

- Updated Microsoft.Azure.WebJobs.Extensions.EventHubs reference to 5.5.0
  - Bug Fix: When binding to a CancellationToken, the token will no longer be signaled when in Drain Mode.
    To detect if the function app is in Drain Mode, use dependency injection to inject the IDrainModeManager,
    and check the IsDrainModeEnabled property.
