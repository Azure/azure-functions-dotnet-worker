## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Azure.Functions.Sdk <Version>

- Fix Visual Studio zip deployments failing when the deployment endpoint returns an empty response. (#3539)
- Prevented SDK build and publish tasks from leaving scanned assemblies, dependencies, or deployment zip files locked after use. (#3540, #3536)
