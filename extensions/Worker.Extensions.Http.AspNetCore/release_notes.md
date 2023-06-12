## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore

- Create FunctionContext extension to retrieve HttpContext (#1534)
- `UseAspNetCoreIntegration()` and `ConfgigureAspNetCoreIntegration()` are now obsolete. Use `ConfigureFunctionsWebApplication()` to configure services for AspNetCore integration. Details can be found in PR #1601.
