// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker.Extensions.ServiceBus.Testing;
using Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs.Testing;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Testing.Tests.Invocation;

public sealed class ServiceTriggerInvocationTests
{
    [Fact]
    public async Task ServiceBusMessage_UsesProductionSdkConverterAndBindingData()
    {
        await using var factory = CreateFactory();
        ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("payload"),
            messageId: "sdk-message-id",
            deliveryCount: 3,
            lockTokenGuid: Guid.NewGuid(),
            properties: new Dictionary<string, object> { ["source"] = "test" });

        FunctionInvocationResult result = await factory.InvokeServiceBusAsync(
            "ServiceBusMessage",
            "message",
            message);

        Assert.True(
            result.Status == FunctionInvocationStatus.Succeeded,
            $"{result.Exception?.Type}: {result.Exception?.Message}\n{result.Exception?.StackTrace}");
        Assert.Equal(
            "payload|sdk-message-id|sdk-message-id|3|test",
            Assert.IsType<FunctionTestValue.StringValue>(result.ReturnValue).Value);
    }

    [Fact]
    public async Task ServiceBusBatch_PreservesMessageOrderAndIdentity()
    {
        await using var factory = CreateFactory();
        ServiceBusReceivedMessage[] messages =
        [
            CreateMessage("first"),
            CreateMessage("second")
        ];

        FunctionInvocationResult result = await factory.InvokeServiceBusBatchAsync(
            "ServiceBusBatch",
            "messages",
            messages);

        Assert.True(
            result.Status == FunctionInvocationStatus.Succeeded,
            $"{result.Exception?.Type}: {result.Exception?.Message}\n{result.Exception?.StackTrace}");
        Assert.Equal(
            "first,second",
            Assert.IsType<FunctionTestValue.StringValue>(result.ReturnValue).Value);
    }

    [Fact]
    public async Task BlobContent_UsesWorkerConversionAndTriggerBindingData()
    {
        await using var factory = CreateFactory();

        FunctionInvocationResult result = await factory.InvokeBlobAsync(
            "BlobBytes",
            "blob",
            BinaryData.FromString("blob-payload"),
            "order-42.json",
            new Uri("https://account.blob.core.windows.net/testing/order-42.json"));

        Assert.True(
            result.Status == FunctionInvocationStatus.Succeeded,
            $"{result.Exception?.Type}: {result.Exception?.Message}\n{result.Exception?.StackTrace}");
        Assert.Equal(
            "order-42.json|blob-payload",
            Assert.IsType<FunctionTestValue.StringValue>(result.ReturnValue).Value);
    }

    [Fact]
    public async Task BlobClient_UsesProductionBlobExtensionConverterWithoutNetworkAccess()
    {
        await using FunctionsApplicationFactory<ModernFunctionApp.Program> factory = CreateFactory()
            .WithSetting("Storage", "UseDevelopmentStorage=true");

        FunctionInvocationResult result = await factory.InvokeBlobClientAsync(
            "BlobClientIdentity",
            "blob",
            "Storage",
            "testing",
            "folder/order-42.json",
            new Uri("https://account.blob.core.windows.net/testing/folder/order-42.json"));

        Assert.True(
            result.Status == FunctionInvocationStatus.Succeeded,
            $"{result.Exception?.Type}: {result.Exception?.Message}\n{result.Exception?.StackTrace}");
        Assert.Equal(
            "testing|folder/order-42.json|folder/order-42.json",
            Assert.IsType<FunctionTestValue.StringValue>(result.ReturnValue).Value);
    }

    [Fact]
    public void TriggerBuilders_RejectInvalidMessagesAndBlobCoordinates()
    {
        ServiceBusReceivedMessage noLockToken = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("payload"));
        Assert.Throws<ArgumentException>(() => ServiceBusTriggerTestData.Message(noLockToken));
        Assert.Throws<ArgumentException>(() => ServiceBusTriggerTestData.MessageBatch([]));
        Assert.Throws<ArgumentException>(() => BlobTriggerTestData.Client("", "container", "blob"));
        Assert.Throws<ArgumentException>(() => BlobTriggerTestData.Client("Storage", "", "blob"));
        Assert.Throws<ArgumentException>(() => BlobTriggerTestData.Client("Storage", "container", ""));
    }

    private static ServiceBusReceivedMessage CreateMessage(string messageId)
        => ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(messageId),
            messageId: messageId,
            lockTokenGuid: Guid.NewGuid());

    private static FunctionsApplicationFactory<ModernFunctionApp.Program> CreateFactory()
        => new FunctionsApplicationFactory<ModernFunctionApp.Program>()
            .WithContentRoot(GetFunctionOutput());

    private static string GetFunctionOutput()
        => Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Resources",
            "Testing",
            "ModernFunctionApp",
            "bin",
            "Release",
            "net8.0"));
}
