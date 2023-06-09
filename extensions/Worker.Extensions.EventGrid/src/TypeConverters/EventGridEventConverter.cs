// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters
{
    /// <summary>
    /// Converter to bind to EventGridEvent or EventGridEvent[] parameter.
    /// </summary>
    [SupportedConverterType(typeof(EventGridEvent))]
    [SupportedConverterType(typeof(EventGridEvent[]))]
    internal class EventGridEventConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.TargetType != typeof(EventGridEvent) && context.TargetType != typeof(EventGridEvent[]))
            {
                return new(ConversionResult.Unhandled());
            }

            return EventGridHelper.ConvertHelper(context);
        }
    }
}
