## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk 1.18.1

- Updated `Microsoft.Azure.Functions.Worker.Sdk.Generators` reference to 1.3.4.

### Microsoft.Azure.Functions.Worker.Sdk.Generators 1.3.4

- Changed `FunctionExecutorGenerator` to avoid generation of long `if`/`else` chains for apps with a large number of functions.

- Use full namespace for `Task.FromResult` in function metadata provider generator to avoid namespace conflict (#2681)

- <entry>
