// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters
{
    internal static class EventGridHelper
    {
        internal static ValueTask<ConversionResult> DeserializeToTargetType(ConverterContext context)
        {
            try
            {
                if (context?.Source is not string contextSource)
                {
                    return new(ConversionResult.Failed(new InvalidOperationException("Context source must be a non-null string. Current type of context source is " + context?.Source?.GetType())));
                }
                
                var targetType = context!.TargetType;
                var item = JsonSerializer.Deserialize(contextSource, targetType);
                return new(ConversionResult.Success(item));
            }
            catch (JsonException ex)
            {
                string msg = String.Format(CultureInfo.CurrentCulture,
                    @"Binding parameters to complex objects uses JSON serialization.
                    1. Bind the parameter type as 'string' instead to get the raw values and avoid JSON deserialization, or
                    2. Change the event payload to be valid json.");

                return new(ConversionResult.Failed(new InvalidOperationException(msg, ex)));
            }
            catch (Exception ex)
            {
                return new(ConversionResult.Failed(ex));
            }
        }
    }
}
