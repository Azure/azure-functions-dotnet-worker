## What's Changed

### Microsoft.Azure.Functions.Worker.OpenTelemetry <version>

- Add `cloud.account.id`, `azure.resource_group.name`, and `faas.instance` resource attributes when their Azure environment variables are available. Explicit values in `OTEL_RESOURCE_ATTRIBUTES` take precedence.