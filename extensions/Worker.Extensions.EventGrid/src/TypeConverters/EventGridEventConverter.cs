// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters
{
    /// <summary>
    /// Converter to bind to EventGridEvent parameter.
    /// </summary>
    [SupportedConverterType(typeof(EventGridEvent))]
    internal class EventGridEventConverter : EventGridConverterBase<EventGridEvent>
    {
        public EventGridEventConverter(ILogger<EventGridEventConverter> logger)
            : base(logger)
        {
        }

        public override ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (!CanConvert(context))
            {
                return new(ConversionResult.Unhandled());
            }
            try
            {
                var contextSource = context?.Source as string;

                if (contextSource is not null)
                {
                    var cloudEvent = JsonSerializer.Deserialize<EventGridEvent>(contextSource);
                    return new(ConversionResult.Success(cloudEvent));
                }
            }
            catch (Exception ex)
            {
                return new(ConversionResult.Failed(ex));
            }

            return new(ConversionResult.Unhandled());
        }
    }
}
