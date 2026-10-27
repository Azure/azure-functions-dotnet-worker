# Microsoft.Azure.Functions.Worker.OpenTelemetry

This package adds extension methods and services to configure OpenTelemetry for use in Azure Functions .NET isolated applications.

This package does **not** add OpenTelemetry services directly. This must be done directly. Instead, this package only configures isolated application and resource detector.

## Getting Started

1. Add packages

``` CSharp
dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore
dotnet add package Microsoft.Azure.Functions.Worker.OpenTelemetry --prerelease
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

### Resource identity attributes

When `WEBSITE_SITE_NAME` is non-empty, the resource detector also adds:

| Attribute | Source |
| --- | --- |
| `cloud.account.id` | The subscription ID in `WEBSITE_OWNER_NAME`, before the first `+`. |
| `azure.resource_group.name` | `WEBSITE_RESOURCE_GROUP`. |
| `faas.instance` | The first non-empty value of `WEBSITE_INSTANCE_ID`, `WEBSITE_POD_NAME`, or `CONTAINER_NAME`, in that order. |

Missing or empty values are omitted. Values explicitly configured through
`OTEL_RESOURCE_ATTRIBUTES` take precedence over these three detected attributes.
On Flex Consumption, `WEBSITE_RESOURCE_GROUP` may be unavailable; the detector
still emits the subscription and instance when available, without inferring a
resource group from `WEBSITE_OWNER_NAME`.


