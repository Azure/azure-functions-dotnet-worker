# Microsoft.Azure.Functions.Worker.OpenTelemetry

This package adds extension methods and services to configure OpenTelemetry for use in Azure Functions .NET isolated applications.

This package does **not** add OpenTelemetry services directly. This must be done directly. Instead, this package only configures isolated application and resource detector.

## Getting Started

1. Add packages

``` CSharp
dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore
dotnet add package Microsoft.Azure.Functions.Worker.OpenTelemetry
```

2. Configure ApplicationInsights using Azure Monitor OpenTelemetry Distro

``` CSharp
services.AddOpenTelemetry()
 .UseFunctionsWorkerDefaults()
 .UseAzureMonitor();
```

## UseFunctionsWorkerDefaults

UseFunctionsWorkerDefaults() method will configure the following:
1. Resource detector for Azure Functions
2. Adds a capability to avoid duplicate telemetry


