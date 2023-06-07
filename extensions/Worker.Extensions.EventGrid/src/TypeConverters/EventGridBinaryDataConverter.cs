// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters
{
    /// <summary>
    /// Converter to bind to BinaryData parameter.
    /// </summary>
    [SupportedConverterType(typeof(BinaryData))]
    internal class EventGridBinaryDataConverter : EventGridConverterBase<BinaryData>
    {
        public EventGridBinaryDataConverter(ILogger<EventGridBinaryDataConverter> logger)
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
                    var cloudEvent = BinaryData.FromObjectAsJson<string>(contextSource);
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
