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
    [SupportedConverterType(typeof(EventGridEvent))]
    [SupportedConverterType(typeof(EventGridEvent[]))]
    internal class EventGridEventConverter : EventGridConverterBase
    {
        protected override object ConvertCoreAsync(Type targetType, string json)
        {
            if (targetType != typeof(EventGridEvent) && targetType != typeof(EventGridEvent[]))
            {
                return ConversionResult.Unhandled();
            }

            return JsonSerializer.Deserialize(json, targetType)!;
        }
    }
}
