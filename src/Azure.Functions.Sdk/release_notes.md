## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Azure.Functions.Sdk 1.0.2

- Target the generated extensions project at the highest framework supported by the selected .NET SDK, capped at `net10.0`. SDKs 8 and 9 use `net8.0` and `net9.0`, respectively, without requiring the .NET 10 SDK (#3447)
