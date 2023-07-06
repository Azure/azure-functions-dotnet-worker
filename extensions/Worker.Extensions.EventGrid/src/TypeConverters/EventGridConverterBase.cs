// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker
{
    internal abstract class EventGridConverterBase  : IInputConverter
    {
        public EventGridConverterBase() { }

        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                if (context.Source is not string json)
                {
                    throw new InvalidOperationException("Context source must be a non-null string");
                }

                var result = ConvertCore(context.TargetType, json);
                return new ValueTask<ConversionResult>(result);
            }
            catch (JsonException ex)
            {
                string msg = String.Format(CultureInfo.CurrentCulture,
                    @"Binding parameters to complex objects uses JSON serialization.
                    1. Bind the parameter type as 'string' instead to get the raw values and avoid JSON deserialization, or
                    2. Change the queue payload to be valid json.");

                return new ValueTask<ConversionResult>(ConversionResult.Failed(new InvalidOperationException(msg, ex)));
            }
            catch (Exception ex)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Failed(ex));
            }
        }

        protected abstract ConversionResult ConvertCore(Type targetType, string json);
    }
}
