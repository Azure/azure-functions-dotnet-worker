// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker.Testing;

namespace Microsoft.Azure.Functions.Worker.Extensions.ServiceBus.Testing;

/// <summary>Creates worker-protocol input for a synthetic Service Bus trigger invocation.</summary>
public static class ServiceBusTriggerTestData
{
    private const string BindingSource = "AzureServiceBusReceivedMessage";
    private const string BinaryContentType = "application/octet-stream";
    private const int LockTokenLength = 16;

    /// <summary>Creates input for a function parameter bound as <see cref="ServiceBusReceivedMessage"/>.</summary>
    public static FunctionTestValue Message(ServiceBusReceivedMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return new FunctionTestValue.ModelBindingValue(ToModelBindingData(message));
    }

    /// <summary>Creates input for a function parameter bound as a batch of <see cref="ServiceBusReceivedMessage"/> values.</summary>
    public static FunctionTestValue MessageBatch(IReadOnlyList<ServiceBusReceivedMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        if (messages.Count == 0)
        {
            throw new ArgumentException("A Service Bus trigger batch cannot be empty.", nameof(messages));
        }

        var values = new FunctionTestModelBindingData[messages.Count];
        for (int index = 0; index < messages.Count; index++)
        {
            ServiceBusReceivedMessage message = messages[index]
                ?? throw new ArgumentException("A Service Bus trigger batch cannot contain null messages.", nameof(messages));
            values[index] = ToModelBindingData(message);
        }

        return new FunctionTestValue.ModelBindingCollection(Array.AsReadOnly(values));
    }

    /// <summary>Creates input for a function parameter bound to the Service Bus message body as binary data.</summary>
    public static FunctionTestValue Body(ServiceBusReceivedMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return FunctionTestValue.Bytes(message.Body.ToMemory());
    }

    /// <summary>Adds the standard host-supplied Service Bus binding data for a message.</summary>
    public static FunctionInvocationRequest WithMessageMetadata(
        this FunctionInvocationRequest request,
        ServiceBusReceivedMessage message)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(message);

        request = AddString(request, "MessageId", message.MessageId);
        request = AddString(request, "CorrelationId", message.CorrelationId);
        request = AddString(request, "ContentType", message.ContentType);
        request = AddString(request, "Subject", message.Subject);
        request = AddString(request, "To", message.To);
        request = AddString(request, "ReplyTo", message.ReplyTo);
        request = AddString(request, "SessionId", message.SessionId);
        request = AddString(request, "ReplyToSessionId", message.ReplyToSessionId);
        request = AddString(request, "PartitionKey", message.PartitionKey);
        request = AddString(request, "TransactionPartitionKey", message.TransactionPartitionKey);
        request = AddString(request, "DeadLetterSource", message.DeadLetterSource);
        request = request
            .WithBindingData("DeliveryCount", FunctionTestValue.String(message.DeliveryCount.ToString(CultureInfo.InvariantCulture)))
            .WithBindingData("SequenceNumber", FunctionTestValue.String(message.SequenceNumber.ToString(CultureInfo.InvariantCulture)))
            .WithBindingData("EnqueuedSequenceNumber", FunctionTestValue.String(message.EnqueuedSequenceNumber.ToString(CultureInfo.InvariantCulture)));

        if (message.LockedUntil != default)
        {
            request = request.WithBindingData("LockedUntil", FunctionTestValue.String(message.LockedUntil.UtcDateTime.ToString("O")));
        }

        if (message.EnqueuedTime != default)
        {
            request = request.WithBindingData("EnqueuedTimeUtc", FunctionTestValue.String(message.EnqueuedTime.UtcDateTime.ToString("O")));
        }

        return request;
    }

    private static FunctionInvocationRequest AddString(
        FunctionInvocationRequest request,
        string name,
        string? value)
        => value is null ? request : request.WithBindingData(name, FunctionTestValue.String(value));

    private static FunctionTestModelBindingData ToModelBindingData(ServiceBusReceivedMessage message)
    {
        ReadOnlyMemory<byte> messageBytes = message.GetRawAmqpMessage().ToBytes().ToMemory();
        if (!Guid.TryParse(message.LockToken, out Guid lockToken) || lockToken == Guid.Empty)
        {
            throw new ArgumentException(
                "The Service Bus message must have a GUID lock token. Create test messages with ServiceBusModelFactory.ServiceBusReceivedMessage(lockTokenGuid: ...).",
                nameof(message));
        }

        byte[] content = new byte[LockTokenLength + messageBytes.Length];
        lockToken.TryWriteBytes(content);
        messageBytes.CopyTo(content.AsMemory(LockTokenLength));
        return new FunctionTestModelBindingData("1.0", BindingSource, BinaryContentType, content);
    }
}
