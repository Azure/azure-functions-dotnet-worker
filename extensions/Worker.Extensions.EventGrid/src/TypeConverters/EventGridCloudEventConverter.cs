// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters
{
    /// <summary>
    /// Converter to bind to <see cref="CloudEvent" /> or <see cref="CloudEvent[]" /> type parameters.
    /// </summary>
    [SupportedConverterType(typeof(CloudEvent))]
    [SupportedConverterType(typeof(CloudEvent[]))]
    internal class EventGridCloudEventConverter: EventGridConverterBase
    {
        protected override object ConvertCoreAsync(Type targetType, string json)
        {
            if (targetType != typeof(CloudEvent) && targetType != typeof(CloudEvent[]))
            {
                return ConversionResult.Unhandled();
            }

            return JsonSerializer.Deserialize(json, targetType)!;
        }
    }
}
