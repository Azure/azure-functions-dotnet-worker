# Service trigger testing sample

This xUnit project demonstrates worker-integration tests for Service Bus and Blob triggers.

It covers rich and batched Service Bus input, Blob content and path data, network-free Blob client conversion, and capability-boundary assertions.

```powershell
dotnet build test\Resources\Testing\ModernFunctionApp\ModernFunctionApp.csproj -c Release
dotnet test samples\Testing\ServiceTriggers.Tests\ServiceTriggers.Tests.csproj -c Release
```

Use Core Tools with Azurite or deployed Azure services for listener polling, settlement, locks, receipts, poison handling, host retries, concurrency, scale, identity, or networking.
