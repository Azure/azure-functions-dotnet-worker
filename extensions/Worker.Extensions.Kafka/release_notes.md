## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.Kafka 3.9.0

- Add `BindingCapabilities` attribute to KafkaTrigger to express function-level retry capabilities. (#1457)
- Updated Kafka Extension nuget package to most recent version (3.9.0) (#1713)
- Exposed an important scaling parameter `LagThreshold`` to let a user configure their scaling preferences within Kafka Trigger function. (#1713)
