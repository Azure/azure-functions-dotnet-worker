# Blob trigger testing

This preview companion supplies synthetic Blob trigger content and binding data to `FunctionsApplicationFactory<TEntryPoint>`. Invocations use generated function metadata and the production isolated-worker conversion, middleware, dependency-injection, logging, and execution pipeline.

It does not start a Blob listener or simulate polling, receipts, retries, poison handling, storage writes, concurrency, or scale. Use the real Functions host with Azurite or Azure Storage when those behaviors are under test.

```csharp
FunctionInvocationResult result = await factory.InvokeBlobAsync(
    "ProcessBlob",
    "blob",
    BinaryData.FromString("payload"),
    "orders/42.json",
    new Uri("https://account.blob.core.windows.net/input/orders/42.json"));

result.EnsureSucceeded();
```

For complete xUnit examples covering content binding, path tokens, network-free SDK-client conversion, and capability boundaries, see `samples/Testing/ServiceTriggers.Tests` in the worker repository.
