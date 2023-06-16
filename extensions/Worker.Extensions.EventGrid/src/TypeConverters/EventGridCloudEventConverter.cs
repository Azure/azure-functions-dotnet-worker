// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters
{
    /// <summary>
    /// Converter to bind to CloudEvent or CloudEvent[] parameter.
    /// </summary>
    [SupportedConverterType(typeof(CloudEvent))]
    [SupportedConverterType(typeof(CloudEvent[]))]
    internal class EventGridCloudEventConverter: IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (context is null)
            {
                return new(ConversionResult.Failed(new ArgumentNullException(nameof(context))));
            }

            if (context.TargetType != typeof(CloudEvent) && context.TargetType != typeof(CloudEvent[]))
            {
                return new(ConversionResult.Unhandled());
            }

            return EventGridHelper.DeserializeToTargetType(context);
        }
    }
}
