## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore <version>

- Fixed a bug that would lead to an empty exception message in some model binding failures.
- Cookies will default to `Secure = true` if not set explicitly
