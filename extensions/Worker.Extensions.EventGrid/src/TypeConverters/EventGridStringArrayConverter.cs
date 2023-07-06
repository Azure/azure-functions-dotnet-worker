// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters
{
    /// <summary>
    /// Converter to bind to <see cref="string[]" /> type parameters.
    /// </summary>
    [SupportedConverterType(typeof(string[]))]
    internal class EventGridStringArrayConverter : EventGridConverterBase
    {
        protected override ConversionResult ConvertCore(Type targetType, string json)
        {
            if (targetType != typeof(string[]))
            {
                return ConversionResult.Unhandled();
            }

            var jsonData = JsonSerializer.Deserialize<List<object>>(json);
            var result = jsonData.Select(d => d.ToString()).ToArray();
            return ConversionResult.Success(result);
        }
    }
}
