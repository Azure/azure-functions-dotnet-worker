using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters
{
    /// <summary>
    /// Converter to bind to CloudEvent parameter.
    /// </summary>
    [SupportsDeferredBinding]
    [SupportedConverterType(typeof(CloudEvent))]
    internal class EventGridCloudEventConverter: EventGridConverterBase<CloudEvent>
    {
        public EventGridCloudEventConverter(ILogger<EventGridCloudEventConverter> logger)
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
                var modelBindingData = context?.Source as ModelBindingData;
                var eventGridData = GetBindingDataContent(modelBindingData);

                /*
                var result = ConvertModelBindingData(eventGridData);

                if (result is not null)
                {
                    return new(ConversionResult.Success(result));
                }
                */
            }
            catch (Exception ex)
            {
                return new(ConversionResult.Failed(ex));
            }

            return new(ConversionResult.Unhandled());
        }
    }
}
