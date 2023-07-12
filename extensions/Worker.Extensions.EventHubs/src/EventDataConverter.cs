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
using Microsoft.Azure.Functions.Worker.Extensions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Converter to bind to <see cref="EventData" /> type or <see cref="T:EventData[]" />  parameters.
    /// </summary>
    [SupportsDeferredBinding]
    [SupportedTargetType(typeof(EventData))]
    [SupportedTargetType(typeof(EventData[]))]
    internal class EventDataConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            try
            {
                ConversionResult result = context?.Source switch
                {
                    ModelBindingData binding => ConversionResult.Success(ConvertToEventData(binding)),
                    // Only array collections are currently supported, which matches the behavior of the in-proc extension.
                    CollectionModelBindingData collection => ConversionResult.Success(collection.ModelBindingData
                        .Select(ConvertToEventData).ToArray()),
                    _ => ConversionResult.Unhandled()
                };
                return new ValueTask<ConversionResult>(result);
            }
            catch (Exception exception)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Failed(exception));
            }
        }

        private EventData ConvertToEventData(ModelBindingData binding)
        {
            if (binding is null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            if (binding.Source is not Constants.BindingSource)
            {
                throw new InvalidBindingSourceException(Constants.BindingSource);
            }

            if (binding.ContentType is not Constants.BinaryContentType)
            {
                throw new InvalidContentTypeException(Constants.BinaryContentType);
            }

            return new EventData(AmqpAnnotatedMessage.FromBytes(binding.Content));
        }
    }
}