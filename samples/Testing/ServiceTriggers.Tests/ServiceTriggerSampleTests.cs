using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker.Extensions.ServiceBus.Testing;
using Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs.Testing;
using Microsoft.Azure.Functions.Worker.Testing;
using Xunit;

namespace ServiceTriggers.Tests;

public sealed class ServiceTriggerFixture : IAsyncLifetime
{
    public FunctionsApplicationFactory<ModernFunctionApp.Program> Factory { get; } =
        new FunctionsApplicationFactory<ModernFunctionApp.Program>()
            .WithSetting("Storage", "UseDevelopmentStorage=true")
            .WithContentRoot(Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..", "..",
                "test", "Resources", "Testing", "ModernFunctionApp",
                "bin", "Release", "net8.0")));

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await Factory.DisposeAsync();
}

public sealed class ServiceTriggerSampleTests(ServiceTriggerFixture fixture)
    : IClassFixture<ServiceTriggerFixture>
{
    [Fact]
    public async Task ServiceBusMessage_ConvertsSdkModelAndBindingMetadata()
    {
        ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("order-created"),
            messageId: "message-42",
            deliveryCount: 2,
            lockTokenGuid: Guid.NewGuid(),
            properties: new Dictionary<string, object> { ["source"] = "sample" });

        FunctionInvocationResult result = await fixture.Factory.InvokeServiceBusAsync(
            "ServiceBusMessage",
            "message",
            message);

        result.EnsureSucceeded();
        Assert.Equal(
            "order-created|message-42|message-42|2|sample",
            Assert.IsType<FunctionTestValue.StringValue>(result.ReturnValue).Value);
    }

    [Fact]
    public async Task ServiceBusBatch_PreservesMessageOrder()
    {
        ServiceBusReceivedMessage[] messages =
        [
            CreateMessage("first"),
            CreateMessage("second")
        ];

        FunctionInvocationResult result = await fixture.Factory.InvokeServiceBusBatchAsync(
            "ServiceBusBatch",
            "messages",
            messages);

        result.EnsureSucceeded();
        Assert.Equal(
            "first,second",
            Assert.IsType<FunctionTestValue.StringValue>(result.ReturnValue).Value);
    }

    [Fact]
    public async Task BlobContent_BindsPayloadAndPathToken()
    {
        FunctionInvocationResult result = await fixture.Factory.InvokeBlobAsync(
            "BlobBytes",
            "blob",
            BinaryData.FromString("invoice-content"),
            "invoices/42.txt",
            new Uri("https://account.blob.core.windows.net/testing/invoices/42.txt"));

        result.EnsureSucceeded();
        Assert.Equal(
            "invoices/42.txt|invoice-content",
            Assert.IsType<FunctionTestValue.StringValue>(result.ReturnValue).Value);
    }

    [Fact]
    public async Task BlobClient_BindsIdentityWithoutContactingStorage()
    {
        FunctionInvocationResult result = await fixture.Factory.InvokeBlobClientAsync(
            "BlobClientIdentity",
            "blob",
            "Storage",
            "testing",
            "invoices/42.txt",
            new Uri("https://account.blob.core.windows.net/testing/invoices/42.txt"));

        result.EnsureSucceeded();
        Assert.Equal(
            "testing|invoices/42.txt|invoices/42.txt",
            Assert.IsType<FunctionTestValue.StringValue>(result.ReturnValue).Value);
    }

    [Fact]
    public void CapabilityBoundary_RemainsWorkerOnly()
    {
        Assert.False(fixture.Factory.Capabilities.SupportsTriggerListeners);
        Assert.False(fixture.Factory.Capabilities.SupportsMessageSettlement);
        Assert.False(fixture.Factory.Capabilities.SupportsRetryScheduling);
    }

    private static ServiceBusReceivedMessage CreateMessage(string messageId)
        => ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(messageId),
            messageId: messageId,
            lockTokenGuid: Guid.NewGuid());
}
