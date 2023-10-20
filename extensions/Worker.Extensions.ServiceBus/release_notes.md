## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.ServiceBus 5.14.1

- Fixed issue where deadlettering a message without specifying properties to modify could throw
  an exception from out of proc extension.
- Include underlying exception details in RpcException when a failure occurs.
