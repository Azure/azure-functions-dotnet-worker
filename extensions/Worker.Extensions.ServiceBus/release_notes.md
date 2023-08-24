## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.ServiceBus 5.13.0

- Updated Microsoft.Azure.WebJobs.Extensions.ServiceBus reference to 5.12.0
  - Bug Fix: When binding to a CancellationToken, the token will no longer be signaled when in Drain Mode.
    To detect if the function app is in Drain Mode, use dependency injection to inject the IDrainModeManager,
    and check the IsDrainModeEnabled property.
