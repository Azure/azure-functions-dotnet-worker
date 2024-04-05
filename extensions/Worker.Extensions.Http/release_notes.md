## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.Http 3.2.0

- Added ability to bind a POCO parameter to the request body using `FromBodyAttribute`
  - Special thanks to @njqdev for the contributions and collaboration on this feature
- Introduces `HttpResultAttribute`, which should be used to label the parameter associated with the HTTP result in multiple output binding scenarios (#2322).
