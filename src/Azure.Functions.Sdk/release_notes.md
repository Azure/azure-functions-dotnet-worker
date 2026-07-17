## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Azure.Functions.Sdk <version>

- fix: reliably resolve extension restore sources (#3393)
- feat: emit `AZFW0110` warning when the deprecated `FunctionsEnableWorkerIndexing` property is set (#3395)
- fix: redact sensitive information (credentials and query strings) from ZipDeploy publish logs (#3398)
- feat: improve implicit package reference behavior (#3409)
  - now respects central package management
  - no longer emits a warning when manually overriding
  - update `Microsoft.Azure.Functions.Worker` to `2.52.0`
- fix: re-generate worker.config.json on publish without build (#3408)
- fix: Perform atomic write in WriteExtensionProject (#3407)
- fix: avoid zip-file conflicts (#3406)
