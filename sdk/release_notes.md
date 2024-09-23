## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Sdk 1.18.0

- Fix incorrect function version in build message (#2606)
- Fix inner build failures when central package management is enabled (#2689)
- Add support to publish a Function App (Flex Consumption) with `ZipDeploy` (#2712)
  - Add `'UseBlobContainerDeploy'` property to identify when to use `OneDeploy` publish API endpoint (`"<publish_url>/api/publish"`)
  - Enhance `ZipDeploy` deployment status logging by appending the `'status_message'` (when defined) to the output messages

### Microsoft.Azure.Functions.Worker.Sdk.Generators <version>

- <entry>
