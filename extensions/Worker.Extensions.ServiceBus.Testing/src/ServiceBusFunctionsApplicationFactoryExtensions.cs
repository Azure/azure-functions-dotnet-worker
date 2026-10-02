// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker.Testing;

namespace Microsoft.Azure.Functions.Worker.Extensions.ServiceBus.Testing;

/// <summary>Provides synthetic Service Bus trigger invocation helpers.</summary>
public static class ServiceBusFunctionsApplicationFactoryExtensions
{
    /// <summary>
    /// Invokes a function through the production worker pipeline with a rich Service Bus message trigger value.
    /// Listener behavior and message settlement are not simulated.
    /// </summary>
    public static Task<FunctionInvocationResult> InvokeServiceBusAsync<TEntryPoint>(
        this FunctionsApplicationFactory<TEntryPoint> factory,
        string functionName,
        string triggerParameterName,
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken = default)
        where TEntryPoint : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(message);
        FunctionInvocationRequest request = FunctionInvocationRequest.Create()
            .WithInput(triggerParameterName, ServiceBusTriggerTestData.Message(message))
            .WithMessageMetadata(message);
        return factory.InvokeAsync(functionName, request, cancellationToken);
    }

    /// <summary>
    /// Invokes a batch-triggered function through the production worker pipeline.
    /// Listener behavior and message settlement are not simulated.
    /// </summary>
    public static Task<FunctionInvocationResult> InvokeServiceBusBatchAsync<TEntryPoint>(
        this FunctionsApplicationFactory<TEntryPoint> factory,
        string functionName,
        string triggerParameterName,
        IReadOnlyList<ServiceBusReceivedMessage> messages,
        CancellationToken cancellationToken = default)
        where TEntryPoint : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        FunctionInvocationRequest request = FunctionInvocationRequest.Create()
            .WithInput(triggerParameterName, ServiceBusTriggerTestData.MessageBatch(messages));
        return factory.InvokeAsync(functionName, request, cancellationToken);
    }
}
