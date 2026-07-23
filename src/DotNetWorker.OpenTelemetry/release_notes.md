## What's Changed

### Microsoft.Azure.Functions.Worker.OpenTelemetry <version>

- `FunctionsResourceDetector` now favors `OTEL_RESOURCE_ATTRIBUTES` — when `deployment.environment` or `deployment.environment.name` is set, the detector skips adding the slot name to `deployment.environment.name` (#3404).