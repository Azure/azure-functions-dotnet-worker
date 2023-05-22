// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core.Amqp;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.ServiceBus;

namespace Microsoft.Azure.Functions.Worker
{

    [SupportsDeferredBinding]
    [SupportedConverterType(typeof(ServiceBusReceivedMessage))]
    [SupportedConverterType(typeof(ServiceBusReceivedMessage[]))]
    internal class ServiceBusReceivedMessageConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            ConversionResult result = context?.Source switch
            {
                ModelBindingData binding => ConversionResult.Success(ConvertToServiceBusReceivedMessage(binding)),
                // Only array collections are currently supported, which matches the behavior of the in-proc extension.
                CollectionModelBindingData collection => ConversionResult.Success(collection.ModelBindingDataArray
                    .Select(ConvertToServiceBusReceivedMessage).ToArray()),
                _ => ConversionResult.Unhandled()
            };
            return new ValueTask<ConversionResult>(result);
        }

        private ServiceBusReceivedMessage ConvertToServiceBusReceivedMessage(ModelBindingData binding)
        {
            // The lock token is a 16 byte GUID
            const int lockTokenLength = 16;

            if (binding.ContentType != Constants.BinaryContentType)
            {
                throw new InvalidOperationException(
                    $"Unexpected content-type. Only '{Constants.BinaryContentType}' is supported.");
            }

            ReadOnlyMemory<byte> bytes = binding.Content.ToMemory();
            ReadOnlyMemory<byte> lockTokenBytes = bytes.Slice(0, lockTokenLength);
            ReadOnlyMemory<byte> messageBytes = bytes.Slice(lockTokenLength, bytes.Length - lockTokenLength);
            return ServiceBusReceivedMessage.FromAmqpMessage(AmqpAnnotatedMessage.FromBytes(BinaryData.FromBytes(messageBytes)),
                BinaryData.FromBytes(lockTokenBytes));
        }
    }
}