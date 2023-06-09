// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core.Amqp;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.Functions.Worker.Extensions.EventHubs;

namespace Microsoft.Azure.Functions.Worker
{
    [SupportsDeferredBinding]
    [SupportedConverterType(typeof(EventData))]
    [SupportedConverterType(typeof(EventData[]))]
    internal class EventDataConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            ConversionResult result = context?.Source switch
            {
                ModelBindingData binding => ConversionResult.Success(ConvertToEventData(binding)),
                // Only array collections are currently supported, which matches the behavior of the in-proc extension.
                CollectionModelBindingData collection => ConversionResult.Success(collection.ModelBindingDataArray
                    .Select(ConvertToEventData).ToArray()),
                _ => ConversionResult.Unhandled()
            };
            return new ValueTask<ConversionResult>(result);
        }

        private EventData ConvertToEventData(ModelBindingData binding)
        {
            if (binding.ContentType != Constants.BinaryContentType)
            {
                throw new InvalidOperationException(
                    $"Unexpected content-type. Only '{Constants.BinaryContentType}' is supported.");
            }

            return new EventData(AmqpAnnotatedMessage.FromBytes(binding.Content));
        }
    }
}