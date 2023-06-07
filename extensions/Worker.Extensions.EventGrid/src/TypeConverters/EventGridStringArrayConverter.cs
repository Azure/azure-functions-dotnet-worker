// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Functions.Worker.Extensions.EventGrid.TypeConverters
{
    /// <summary>
    /// Converter to bind to string[] parameter.
    /// </summary>
    [SupportedConverterType(typeof(string[]))]
    internal class EventGridStringArrayConverter : EventGridConverterBase<string[]>
    {
        public EventGridStringArrayConverter(ILogger<EventGridStringArrayConverter> logger)
            : base(logger)
        {
        }

        public override ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (!CanConvert(context))
            {
                return new(ConversionResult.Unhandled());
            }
            try
            {
                var contextSource = context?.Source as string;

                if (contextSource is not null)
                {
                    var jsonData = JsonConvert.DeserializeObject<List<object>>(contextSource);
                    List<string> stringList = new List<string>();

                    if (jsonData is not null)
                    {
                        foreach (var item in jsonData)
                        {
                            if (item is not null)
                            {
                                var data = JsonConvert.SerializeObject(item);
                                stringList.Add(data);
                            }
                        }
                        return new(ConversionResult.Success(stringList.ToArray()));
                    }
                }
            }
            catch (Exception ex)
            {
                return new(ConversionResult.Failed(ex));
            }

            return new(ConversionResult.Unhandled());
        }
    }
}
