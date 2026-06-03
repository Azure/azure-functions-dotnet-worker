## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Azure.Functions.Sdk <version>

- feat: improve implicit package reference behavior (#3409)
  - now respects central package management
  - no longer emits a warning when manually overriding
  - update `Microsoft.Azure.Functions.Worker` to `2.52.0`
- fix: re-generate worker.config.json on publish without build (#3408)
- fix: Perform atomic write in WriteExtensionProject (#3407)
- fix: avoid zip-file conflicts (#3406)
