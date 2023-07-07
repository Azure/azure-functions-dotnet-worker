// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core.Amqp;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.ServiceBus;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Converter to bind to <see cref="ServiceBusReceivedMessage" /> or <see cref="ServiceBusReceivedMessage[]" /> type parameters.
    /// </summary>
    [SupportsDeferredBinding]
    [SupportedTargetType(typeof(ServiceBusReceivedMessage))]
    [SupportedTargetType(typeof(ServiceBusReceivedMessage[]))]
    internal class ServiceBusReceivedMessageConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            try
            {
                ConversionResult result = context?.Source switch
                {
                    ModelBindingData binding => ConversionResult.Success(ConvertToServiceBusReceivedMessage(binding)),
                    // Only array collections are currently supported, which matches the behavior of the in-proc extension.
                    CollectionModelBindingData collection => ConversionResult.Success(collection.ModelBindingData
                        .Select(ConvertToServiceBusReceivedMessage).ToArray()),
                    _ => ConversionResult.Unhandled()
                };
                return new ValueTask<ConversionResult>(result);
            }
            catch (Exception exception)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Failed(exception));
            }
        }

        private ServiceBusReceivedMessage ConvertToServiceBusReceivedMessage(ModelBindingData binding)
        {
            if (binding is null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            if (binding.Source is not Constants.BindingSource)
            {
                throw new InvalidBindingSourceException(binding.Source, Constants.BindingSource);
            }

            if (binding.ContentType is not Constants.BinaryContentType)
            {
                throw new InvalidContentTypeException(binding.ContentType, Constants.BinaryContentType);
            }

            // The lock token is a 16 byte GUID
            const int lockTokenLength = 16;

            ReadOnlyMemory<byte> bytes = binding.Content.ToMemory();
            ReadOnlyMemory<byte> lockTokenBytes = bytes.Slice(0, lockTokenLength);
            ReadOnlyMemory<byte> messageBytes = bytes.Slice(lockTokenLength, bytes.Length - lockTokenLength);
            return ServiceBusReceivedMessage.FromAmqpMessage(AmqpAnnotatedMessage.FromBytes(BinaryData.FromBytes(messageBytes)),
                BinaryData.FromBytes(lockTokenBytes));
        }
    }
}