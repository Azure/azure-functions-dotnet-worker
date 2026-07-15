## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Azure.Functions.Sdk <version>

- fix: no longer add `Microsoft.Azure.Functions.Worker` as an implicit package reference, which caused silent version downgrades and runtime `MissingMethodException` failures (#3450)
  - reference `Microsoft.Azure.Functions.Worker` explicitly in your project
  - emit `AZFW0111` warning when no worker package is found after restore
- feat: emit `AZFW0110` warning when the deprecated `FunctionsEnableWorkerIndexing` property is set (#3395)
- feat: improve implicit package reference behavior (#3409)
  - now respects central package management
- fix: re-generate worker.config.json on publish without build (#3408)
- fix: Perform atomic write in WriteExtensionProject (#3407)
- fix: avoid zip-file conflicts (#3406)
