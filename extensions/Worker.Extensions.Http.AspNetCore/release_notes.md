## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore <version>

- New overload added to `ConfigureFunctionsWebApplication` to take a `HostBuilderContext` (#1925). Thank you @vmcbaptista
- Added support for the `HttpRequestData` and `HttpResponseData` models, backed by ASP.NET Core. (#1932)

### Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Analyzers

- Analyzer to detect missing ASP.NET Core Integration registration (#1917)
- Code fix suggestion for correct registration in ASP.NET core integration (#1992)