﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Newtonsoft.Json;

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
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.TargetType != typeof(BinaryData) && context.TargetType != typeof(BinaryData[]))
            {
                return new(ConversionResult.Unhandled());
            }

            try
            {
                var contextSource = context?.Source as string;

                if (contextSource is not null)
                {
                   if (context!.TargetType == typeof(BinaryData))
                   {
                        var binaryData = BinaryData.FromObjectAsJson<string>(contextSource);
                        return new(ConversionResult.Success(binaryData));
                   }
                   else if (context!.TargetType == typeof(BinaryData[]))
                   {
                        var jsonData = JsonConvert.DeserializeObject<List<object>>(contextSource);
                        List<BinaryData> binaryDataList = new List<BinaryData>();

                        if (jsonData is not null)
                        {
                            foreach (var item in jsonData)
                            {
                                if (item is not null)
                                {
                                    var binaryData = new BinaryData(JsonConvert.SerializeObject(item));
                                    binaryDataList.Add(binaryData);

                                }
                            }
                            return new(ConversionResult.Success(binaryDataList.ToArray()));
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                string msg = String.Format(CultureInfo.CurrentCulture,
                    @"Binding parameters to complex objects uses Json.NET serialization.
                    1. Bind the parameter type as 'string' instead to get the raw values and avoid JSON deserialization, or
                    2. Change the queue payload to be valid json.
                    The JSON parser failed: {0}",
                    ex.Message);

                return new(ConversionResult.Failed(new InvalidOperationException(msg)));
            }
            catch (Exception ex)
            {
                return new(ConversionResult.Failed(ex));
            }

            return new(ConversionResult.Unhandled());
        }
    }
}
