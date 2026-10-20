# Azure Functions ASP.NET Core testing

`Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Testing` adds explicit ASP.NET Core `TestServer` integration to `Microsoft.Azure.Functions.Worker.Testing`.

Call `WithAspNetCore()` before starting the factory. Requests use worker-owned ASP.NET Core endpoints without opening a TCP listener. Functions-host authorization and other host-owned behavior still require Core Tools, Aspire, or a deployed test environment.

See the repository's `docs/testing.md` for examples and compatibility details.
