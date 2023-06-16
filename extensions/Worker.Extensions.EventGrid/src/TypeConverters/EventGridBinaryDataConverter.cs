// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters
{
    /// <summary>
    /// Converter to bind to BinaryData or BinaryData[] parameter.
    /// </summary>
    [SupportedConverterType(typeof(BinaryData))]
    [SupportedConverterType(typeof(BinaryData[]))]
    internal class EventGridBinaryDataConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            try
            {
                if (context is null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                if (context.Source is not string contextSource)
                {
                    return new(ConversionResult.Failed(new InvalidOperationException("Context source must be a non-null string. Current type of context source is " + context?.Source?.GetType())));
                }

                var targetType = context.TargetType;

                switch (targetType)
                {
                    case Type t when t == typeof(BinaryData):
                        return new(ConversionResult.Success((BinaryData.FromString(contextSource))));
                    case Type t when t == typeof(BinaryData[]):
                        return new(ConversionResult.Success(ConvertToBinaryDataArray(contextSource)));
                }
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

            return new(ConversionResult.Unhandled());
        }

        private BinaryData?[]? ConvertToBinaryDataArray(string contextSource)
        {
            var jsonData = JsonSerializer.Deserialize(contextSource, typeof(List<object>)) as List<object>;
            List<BinaryData?> binaryDataList = new List<BinaryData?>();

            if (jsonData is not null)
            {
                foreach (var item in jsonData)
                {
                    var binaryData = item == null? null: BinaryData.FromString(item.ToString());
                    binaryDataList.Add(binaryData);   
                }
            }

            return binaryDataList.ToArray();
        }
    }
}
