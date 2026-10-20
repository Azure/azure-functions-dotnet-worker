# Service Bus trigger testing

This preview companion supplies synthetic `ServiceBusReceivedMessage` and batch trigger inputs to `FunctionsApplicationFactory<TEntryPoint>`. Invocations use generated function metadata and the production isolated-worker conversion, middleware, dependency-injection, logging, and execution pipeline.

It does not start a Service Bus listener or simulate lock renewal, completion, abandonment, dead-lettering, retries, sessions, concurrency, or scale. Use the real Functions host with an emulator or Azure Service Bus when those behaviors are under test.

```csharp
ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
    body: BinaryData.FromString("payload"),
    messageId: "message-1",
    lockTokenGuid: Guid.NewGuid());

FunctionInvocationResult result = await factory.InvokeServiceBusAsync(
    "ProcessMessage",
    "message",
    message);

result.EnsureSucceeded();
```

For complete xUnit examples covering rich messages, batches, binding metadata, and capability boundaries, see `samples/Testing/ServiceTriggers.Tests` in the worker repository.
