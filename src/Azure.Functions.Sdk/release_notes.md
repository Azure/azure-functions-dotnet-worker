## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Azure.Functions.Sdk 1.1.0

- Support standalone container publishing with `FunctionsContainerMode`, while keeping host-embedded images as the default. Standalone container images omit `worker.config.json`.
- Generated extension projects now declare the SDK package version, allowing direct builds to resolve the SDK and report AZFW0109 without requiring a `global.json` pin. (#3541)
