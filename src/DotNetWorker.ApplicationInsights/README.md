# Microsoft.Azure.Functions.Worker.ApplicationInsights

This package adds extension methods and services to configure ApplicationInsights for use in Azure Functions .NET isolated applications.

This package does **not** add Application Insights services directly. This must be done directly. Instead, this package only augments Application Insights with functions scenarios.

## Getting Started

1. Add packages

``` CSharp
dotnet add package Microsoft.ApplicationInsights.WorkerService
dotnet add package Microsoft.Azure.Functions.Worker.ApplicationInsights --prerelease
```

2. Configure ApplicationInsights

``` CSharp
services.AddApplicationInsightsTelemetryWorkerService();
services.ConfigureFunctionsApplicationInsights();
```

## Distributed Tracing

This package adds an `ITelemetryModule` which listens to the Azure Functions worker `ActivitySource`, converting emitted `Activity`s into `DependencyTelemetry`.

## Logging

This package will adjust logging behavior of the worker to no longer emit logs through the host application. Instead, logs are sent directly to application insights from the worker.

## In-Proc Comparison / Changes

With this package changing the worker to send telemetry directly to application insights, custom `ITelemetryInitializer` or `ITelemetryProcessor` will only apply to worker-originating telemetry. Telemetry which originates from the host process will **not** be ran through the same telemetry pipeline. This means when comapred to an in-proc functions app, you may see some telemetry items missing customizations performed in initialziers or processors. These telemetry items have originated from the host.

## Configuration

See [this document](https://learn.microsoft.com/en-us/azure/azure-monitor/app/worker-service) on configuring Application Insights for dotnet applications.
