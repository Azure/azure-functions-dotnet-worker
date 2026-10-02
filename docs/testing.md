# Integration testing isolated worker applications

The preview worker testing packages run an Azure Functions .NET isolated application in the test process. They are intended for worker integration tests: generated metadata, dependency injection, converters, middleware, user code, output bindings, logs, trace context, and retry context all use the real worker pipeline.

They do not embed or emulate the Azure Functions host.

## Choose the right layer

| Test layer | Use it for | Do not infer |
| --- | --- | --- |
| Unit test | A function method or a small service in isolation | Worker configuration or protocol behavior |
| `FunctionsApplicationFactory<TEntryPoint>` | Worker startup, DI overrides, middleware, converters, synthetic invocations, function-targeted built-in HTTP | URL routing, authorization, listeners, message settlement, retry scheduling, or scale |
| Service Bus and Blob testing companions | SDK-shaped trigger payloads through production worker converters, middleware, DI, user code, outputs, and logs | Listener polling, settlement, lock/receipt management, poison handling, host retries, concurrency, or scale |
| `WithAspNetCore()` | Worker-owned ASP.NET Core routes, binding, middleware, `IActionResult`/`IResult`, and request cancellation through `TestServer` | Functions-host authorization or other host-owned behavior |
| Core Tools or Aspire | Host routing and auth, real trigger listeners, extension behavior, storage settlement, and host retries | Production infrastructure behavior that the chosen emulator does not provide |
| Deployed test environment | Identity, networking, managed services, scale, and production hosting integration | Nothing beyond the environment and scenarios actually exercised |

Synthetic service-trigger invocations are worker integration tests, not end-to-end tests. Use Core Tools with Azurite or the real service when listener, settlement, retry, or durable side effects are the assertion.

## Service Bus and Blob triggers

Add `Microsoft.Azure.Functions.Worker.Extensions.ServiceBus.Testing` or `Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs.Testing` alongside the matching production trigger extension. The helpers preserve SDK model conversion and host-shaped binding data while keeping the host-owned capability boundary explicit.

```csharp
ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
    body: BinaryData.FromString("payload"),
    messageId: "message-1",
    lockTokenGuid: Guid.NewGuid());

FunctionInvocationResult serviceBusResult = await factory.InvokeServiceBusAsync(
    "ProcessMessage", "message", message);

FunctionInvocationResult blobResult = await factory.InvokeBlobAsync(
    "ProcessBlob",
    "blob",
    BinaryData.FromString("payload"),
    "orders/42.json",
    new Uri("https://account.blob.core.windows.net/input/orders/42.json"));
```

Use `ServiceBusTriggerTestData.MessageBatch`/`InvokeServiceBusBatchAsync` for batched messages, `ServiceBusTriggerTestData.Body` for body-only bindings, and `BlobTriggerTestData.Client` when a Blob SDK client parameter is the conversion under test. These APIs intentionally perform no network operations.

### What to test

| Scenario | Test API | Useful assertions |
| --- | --- | --- |
| Rich Service Bus message | `InvokeServiceBusAsync` | Body, message ID, correlation/session data, application properties, delivery count, middleware, logs, outputs |
| Service Bus batch | `InvokeServiceBusBatchAsync` | Message ordering, per-message identity and properties, batch business rules, failure projection |
| Body-only Service Bus binding | `ServiceBusTriggerTestData.Body` with `InvokeAsync` | String, bytes, JSON, or POCO conversion without taking an Azure SDK model dependency in function code |
| Blob content | `InvokeBlobAsync` | Byte/string/POCO content, `{name}` binding data, middleware, output bindings, logs |
| Blob SDK client | `InvokeBlobClientAsync` | Container/blob identity and dependency configuration without a storage request |
| Invalid test data | Trigger data builders | Empty batches, missing Service Bus lock tokens, invalid connection/container/blob names |

For example, this verifies the SDK message converter, application properties, host-shaped metadata, and user-code result:

```csharp
ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
    body: BinaryData.FromString("order-created"),
    messageId: "message-42",
    deliveryCount: 2,
    lockTokenGuid: Guid.NewGuid(),
    properties: new Dictionary<string, object> { ["source"] = "test" });

FunctionInvocationResult result = await factory.InvokeServiceBusAsync(
    "ServiceBusMessage",
    "message",
    message);

result.EnsureSucceeded();
Assert.Contains("message-42", ((FunctionTestValue.StringValue)result.ReturnValue!).Value);
```

This verifies Blob content and a path token without starting Azurite:

```csharp
FunctionInvocationResult result = await factory.InvokeBlobAsync(
    "BlobBytes",
    "blob",
    BinaryData.FromString("invoice-content"),
    "invoices/42.txt",
    new Uri("https://account.blob.core.windows.net/testing/invoices/42.txt"));

result.EnsureSucceeded();
```

The complete runnable project is `samples/Testing/ServiceTriggers.Tests`. It also demonstrates batch input, Blob SDK-client binding, fixture disposal, and capability-boundary assertions.

### What still needs a real host and service

Do not use a successful synthetic invocation as evidence that a Service Bus subscription, Blob listener, or production connection is configured correctly. Escalate to Core Tools/emulators or a deployed environment for:

- queue/topic/subscription selection and listener startup;
- completion, abandonment, dead-lettering, lock renewal, sessions, and duplicate detection;
- Blob polling, Event Grid delivery, receipts, poison handling, and durable output writes;
- host retry scheduling, concurrency controls, scale, identity, networking, and service permissions.

## Built-in HTTP is function-targeted

The built-in HTTP client targets one generated function name. The request URL is payload passed to that function; it is not used to choose a function and no function key is enforced.

```csharp
await using var factory = new FunctionsApplicationFactory<Program>()
    .WithSetting("Sample:Mode", "Test")
    .ConfigureServices(services =>
        services.AddSingleton<IClock>(new FakeClock()));

using HttpClient client = factory.CreateHttpClient("CreateOrder");
using HttpResponseMessage response =
    await client.PostAsJsonAsync("/orders/42?mode=full", new { name = "sample" });

Assert.Equal(HttpStatusCode.Created, response.StatusCode);
```

Inspect `factory.Capabilities` when reusable test infrastructure must choose between the in-process worker and a full host. All host-owned capability flags are deliberately `false`.

## ASP.NET Core integration

Add the companion package and activate it before any property starts the factory:

```csharp
await using var factory = new FunctionsApplicationFactory<Program>()
    .WithSetting("Sample:Mode", "Test")
    .WithAspNetCore();

using HttpClient client = factory.CreateClient();
using HttpResponseMessage response =
    await client.PostAsJsonAsync("/api/orders/42", new { name = "sample" });

response.EnsureSuccessStatusCode();
```

`CreateClient()` is backed by ASP.NET Core `TestServer`; it opens no TCP listener. Worker-owned endpoint routing selects the function. Calling `WithAspNetCore()` repeatedly before startup is idempotent; calling it after startup fails.

## Configuration, lifecycle, and parallel tests

- Create one factory fixture per application configuration and dispose it with `await using` or `IAsyncLifetime`.
- Configuration and service callbacks run before the host service provider is built. A top-level statement that reads configuration imperatively before building the host has already observed the application's original value and cannot be retroactively overridden.
- Clones created by `WithSetting`, `ConfigureServices`, `ConfigureHost`, or `WithAspNetCore` are independent and unstarted.
- Separate factories can run in parallel. The implementation does not write process-global environment variables or `AppContext` values.
- Factory startup is lazy and cached. A startup failure is rethrown consistently; disposal is idempotent and bounds active-invocation shutdown.

## Full-host escalation

Move a test to Core Tools, Aspire, or a deployed environment when it asserts:

- built-in HTTP URL selection, route diagnostics, or function-key authorization;
- timer, queue, topic, blob, or service listeners;
- message completion, abandonment, dead-lettering, or lock renewal;
- host retry scheduling, trigger concurrency, scale, or extension configuration;
- host logs or host-specific exception wire shapes.

The conformance runner in `test/Testing.Conformance/run.ps1` keeps worker transport equivalence separate from a bounded Core Tools/Azurite differential. Missing Core Tools or Azurite may skip the local runtime lane, but the designated Windows CI lane requires it.

## Package and target compatibility

The packages use an independent `1.0.0-preview.n` version line and target .NET 8, .NET 9, and .NET 10. Every package contains a `buildTransitive` compatibility manifest and closed NuGet dependency ranges:

- core testing requires the exact Worker, Worker.Core, and Worker.Grpc build it was compiled against;
- the ASP.NET companion requires the exact core testing and ASP.NET integration builds;
- TestHost is pinned per target framework.

Restore rejects ordinary version skew. Startup also compares referenced and loaded assembly versions and gives an exact-package-set diagnostic if dependencies were forced manually.

## Migrating from community helpers

Keep useful test-data builders and assertions. Replace mocks or copied `FunctionContext` implementations when the test intends to verify worker startup, middleware, conversion, metadata, or protocol-facing results. Replace ad hoc `HttpRequestData` construction with `CreateHttpClient(functionName)` for built-in HTTP, or `WithAspNetCore().CreateClient()` for worker-owned ASP.NET routes. Keep Core Tools tests for host-owned behavior rather than relabeling synthetic invocations as end-to-end coverage.

Runnable source samples are under `samples/Testing`.
