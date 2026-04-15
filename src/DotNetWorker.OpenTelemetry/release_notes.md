## What's Changed

### Microsoft.Azure.Functions.Worker.OpenTelemetry <version>

- Add support for propagating OpenTelemetry baggage to the worker (#3319).
- `FunctionsResourceDetector` now respects `OTEL_SERVICE_NAME` and `OTEL_RESOURCE_ATTRIBUTES` — when either is set, the detector skips adding `service.name` and/or `service.version` so the OTel SDK picks them up without duplication  (#3362).