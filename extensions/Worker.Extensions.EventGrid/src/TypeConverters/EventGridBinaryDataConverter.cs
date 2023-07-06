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
    /// Converter to bind to <see cref="BinaryData" /> or <see cref="BinaryData[]" /> type parameters.
    /// </summary>
    [SupportedConverterType(typeof(BinaryData))]
    [SupportedConverterType(typeof(BinaryData[]))]
    internal class EventGridBinaryDataConverter : EventGridConverterBase
    {
        protected override ConversionResult ConvertCore(Type targetType, string json)
        {
            object result = targetType switch
            {
                Type t when t == typeof(BinaryData) => ConvertToBinaryData(json),
                Type t when t == typeof(BinaryData[]) => () =>
                {
                    var data = JsonSerializer.Deserialize(json, typeof(List<object>)) as List<object>;
                    return data.Select(d => ConvertToBinaryData(d.ToString())).ToArray();
                },
                _ => ConversionResult.Unhandled()
            };

            return ConversionResult.Success(result);
        }

        private BinaryData ConvertToBinaryData(string item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return BinaryData.FromString(item);
        }
    }
}
