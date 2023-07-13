// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters
{
    /// <summary>
    /// Converter to bind to <see cref="EventGridEvent" /> or <see cref="EventGridEvent[]" /> type parameters.
    /// </summary>
    [SupportedTargetType(typeof(EventGridEvent))]
    [SupportedTargetType(typeof(EventGridEvent[]))]
    internal class EventGridEventConverter : EventGridConverterBase
    {
        protected override ConversionResult ConvertCore(Type targetType, string json)
        {
            var eventGridEvent = JsonSerializer.Deserialize(json, targetType);

            if (eventGridEvent is null)
            {
                return ConversionResult.Failed(new Exception("Unable to convert to EventGridEvent."));
            }

            return ConversionResult.Success(eventGridEvent);
        }
    }
}
