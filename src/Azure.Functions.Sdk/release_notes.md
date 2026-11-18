## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Azure.Functions.Sdk <Version>

- Fix Visual Studio zip deployments failing when the deployment endpoint returns an empty response. (#3539)
- Generated extension projects now declare the SDK package version, allowing direct builds to resolve the SDK and report AZFW0109 without requiring a `global.json` pin. (#3541)
