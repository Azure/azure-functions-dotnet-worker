## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore 2.1.2-preview.1

- Fall back to `AppContext.BaseDirectory` for function metadata and HTTP route configuration when `FUNCTIONS_APPLICATION_DIRECTORY` is unset or whitespace. (#3532)
