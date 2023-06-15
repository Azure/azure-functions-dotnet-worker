// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters
{
    /// <summary>
    /// Converter to bind to string[] parameter.
    /// </summary>
    [SupportedConverterType(typeof(string[]))]
    internal class EventGridStringArrayConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (context.TargetType != typeof(string[]))
            {
                return new(ConversionResult.Unhandled());
            }
            try
            {
                var contextSource = context?.Source as string;

                if (contextSource is null)
                {
                    return new(ConversionResult.Failed(new InvalidOperationException("Context source must be a non-null string")));
                }

                var jsonData = JsonSerializer.Deserialize(contextSource, typeof(List<object>)) as List<object>;
                List<string?> stringList = new List<string?>();

                if (jsonData is not null)
                {
                    return new(ConversionResult.Success(jsonData.Select(d => d?.ToString()).ToArray()));
                }
            }
            catch (JsonException ex)
            {
                string msg = String.Format(CultureInfo.CurrentCulture,
                    @"Binding parameters to complex objects uses JSON serialization.
                    1. Bind the parameter type as 'string' instead to get the raw values and avoid JSON deserialization, or
                    2. Change the event payload to be valid json.
                    The JSON parser failed: {0}",
                    ex.Message);

                return new(ConversionResult.Failed(new InvalidOperationException(msg, ex)));
            }
            catch (Exception ex)
            {
                return new(ConversionResult.Failed(ex));
            }

            return new(ConversionResult.Unhandled());
        }
    }
}
