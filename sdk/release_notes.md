## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk 1.14.1

- Update Microsoft.Azure.Functions.Worker.Sdk.Generators dependency to 1.1.1

### Microsoft.Azure.Functions.Worker.Sdk.Generators 1.1.1

- Updated source generated version of function executor to prevent generating type names which causes namespace ambiguity with existing types.(#1847)
- Updated source generated version of function metadata provider to correctly generate binding values for async functions.(#1817)