// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core.Amqp;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Primitives;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;

namespace Microsoft.Azure.Functions.Worker;

internal class ServiceBusReceivedMessageConverter : IInputConverter
{
    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        ConversionResult result = context?.Source switch
        {
            ModelBindingData binding => ConversionResult.Success(ConvertToServiceBusReceivedMessage(binding)),
            CollectionModelBindingData collection when context.TargetType.IsArray => ConversionResult.Success(collection.ModelBindingDataArray.Select(ConvertToServiceBusReceivedMessage).ToArray()),
            CollectionModelBindingData collection => ConversionResult.Success(collection.ModelBindingDataArray.Select(ConvertToServiceBusReceivedMessage).ToList()),
            _ => ConversionResult.Unhandled()
        };
        return new ValueTask<ConversionResult>(result);
    }

    private ServiceBusReceivedMessage ConvertToServiceBusReceivedMessage(ModelBindingData binding)
    {
        // The lock token is a 16 byte GUID
        const int lockTokenLength = 16;

        if (binding.ContentType != "application/octet-stream")
        {
            throw new InvalidOperationException("Only binary data is supported.");
        }

        ReadOnlyMemory<byte> bytes = binding.Content.ToMemory();
        ReadOnlyMemory<byte> lockTokenBytes = bytes.Slice(0, lockTokenLength);
        ReadOnlyMemory<byte> messageBytes = bytes.Slice(lockTokenLength, bytes.Length - lockTokenLength);
        return ServiceBusReceivedMessage.FromAmqpMessage(AmqpAnnotatedMessage.FromBytes(BinaryData.FromBytes(messageBytes)), BinaryData.FromBytes(lockTokenBytes));
    }
}