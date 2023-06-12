## Release notes
<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->
Breaking changes:
- `UseAspNetCoreIntegration()` and `ConfgigureAspNetCoreIntegration()` are now obsolete. Use `ConfigureFunctionsWebApplication()` to configure services for AspNetCore integration. Details can be found in PR #1601.