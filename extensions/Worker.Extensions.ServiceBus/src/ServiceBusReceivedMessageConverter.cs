// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Primitives;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;

namespace Microsoft.Azure.Functions.Worker;

internal class ServiceBusReceivedMessageConverter : IInputConverter
{
    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        if (context is null)
        {
            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }

        return context.Source switch
        {
            ModelBindingData binding => ConvertToServiceBusReceivedMessage(context, binding),
            _ => new ValueTask<ConversionResult>(ConversionResult.Unhandled()),
        };
    }

    private ValueTask<ConversionResult> ConvertToServiceBusReceivedMessage(ConverterContext context, ModelBindingData binding)
    {
        // The lock token is a 16 byte GUID
        const int lockTokenLength = 16;

        ReadOnlyMemory<byte> bytes = binding.Content.ToMemory();
        ReadOnlyMemory<byte> lockTokenBytes = bytes.Slice(0, lockTokenLength);
        ReadOnlyMemory<byte> messageBytes = bytes.Slice(lockTokenLength, bytes.Length - lockTokenLength);
        ServiceBusReceivedMessage message = ServiceBusAmqpExtensions.FromAmqpBytes(BinaryData.FromBytes(messageBytes), BinaryData.FromBytes(lockTokenBytes));
        return new ValueTask<ConversionResult>(ConversionResult.Success(message));
    }
}