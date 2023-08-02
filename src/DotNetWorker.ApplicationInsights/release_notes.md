# Changes

- Added `README.md`

# Breaking Changes

## Removed APIs

- Removed `Microsoft.ApplicationInsights.WorkerService` dependency
- `FunctionsApplicationInsightsExtensions.AddApplicationInsights(IFunctionWorkerApplicationBuilder, Action<ApplicationInsightsServiceOptions>?)`
- `FunctionsApplicationInsightsExtensions.AddApplicationInsightsLogger(IFunctionWorkerApplicationBuilder, Action<ApplicationInsightsLoggerOptions>?)`

## Added APIs

- Added `Microsoft.ApplicationInsights` dependency
- `FunctionsApplicationInsightsExtensions.ConfigureFunctionsApplicationInsights(IServiceCollection)`
