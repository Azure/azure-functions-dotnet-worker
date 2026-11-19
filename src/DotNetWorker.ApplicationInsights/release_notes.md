## What's Changed

### Microsoft.Azure.Functions.Worker.ApplicationInsights 2.52.0

- Added a configurable `MaxTelemetryBufferDelay` option on `FunctionsApplicationInsightsOptions` (default 8 seconds, minimum 5 seconds) to control the Application Insights `ServerTelemetryChannel` flush interval, configurable via `ConfigureFunctionsApplicationInsights(options => ...)`. (#3466)