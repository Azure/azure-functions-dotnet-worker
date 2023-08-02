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
    [SupportedTargetType(typeof(CloudEvent))]
    [SupportedTargetType(typeof(CloudEvent[]))]
    internal class EventGridCloudEventConverter: EventGridConverterBase
    {
        protected override ConversionResult ConvertCore(Type targetType, string json)
        {
            var cloudEvent = JsonSerializer.Deserialize(json, targetType);

            if (cloudEvent is null)
            {
                return ConversionResult.Failed(new Exception("Unable to convert to CloudEvent."));
            }

            return ConversionResult.Success(cloudEvent);
        }
    }
}
