# Azure Functions isolated worker testing

`Microsoft.Azure.Functions.Worker.Testing` is a preview integration-test host for executing an isolated worker application in process.

It loads generated function metadata, runs the real worker middleware and converters, and supports synthetic function invocation plus function-targeted built-in HTTP. It does not provide Functions-host URL routing, authorization, trigger listeners, message settlement, retry scheduling, or scale behavior.

Trigger-specific companions add production-pipeline conversion for Service Bus and Blob trigger payloads without pretending to run host-owned listeners or settlement.

See the repository's `docs/testing.md` for the testing pyramid, lifecycle guidance, examples, and the exact package compatibility policy.
