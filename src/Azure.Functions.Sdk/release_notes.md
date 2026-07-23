## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Azure.Functions.Sdk 0.5.0

- fix: no longer add `Microsoft.Azure.Functions.Worker` as an implicit package reference, which caused silent version downgrades and runtime `MissingMethodException` failures (#3450)
  - reference `Microsoft.Azure.Functions.Worker` explicitly in your project
  - emit `AZFW0111` warning when no worker package is found after restore
  - source generators are skipped if this package is missing
- fix: expand removed properties and pin `Configuration=release` when restoring/resolving the generated extension project (#3399)
- fix: reliably resolve extension restore sources (#3393)
- feat: emit `AZFW0110` warning when the deprecated `FunctionsEnableWorkerIndexing` property is set (#3395)
- fix: redact sensitive information (credentials and query strings) from ZipDeploy publish logs (#3398)
- feat: improve implicit package reference behavior (#3409)
  - now respects central package management
- fix: re-generate worker.config.json on publish without build (#3408)
- fix: Perform atomic write in WriteExtensionProject (#3407)
- fix: avoid zip-file conflicts (#3406)
