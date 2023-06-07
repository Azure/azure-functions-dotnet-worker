// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters
{
    /// <summary>
    /// Converter to bind to CloudEvent parameter.
    /// </summary>
    [SupportedConverterType(typeof(CloudEvent[]))]
    internal class EventGridMultipleCloudEventConverter : EventGridConverterBase<CloudEvent[]>
    {
        public EventGridMultipleCloudEventConverter(ILogger<EventGridMultipleCloudEventConverter> logger)
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
                    var cloudEvents = JsonSerializer.Deserialize<CloudEvent[]>(contextSource);
                    
                    return new(ConversionResult.Success(cloudEvents));
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
