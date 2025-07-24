// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class TypeConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            // Option 2: Check the source for null or empty and return unhandled if so.
            // However with this approach we would need to ensure all Converters handle this scenario.
            Type? sourceType = context.Source?.GetType();

            if (sourceType is not null &&
                context.TargetType.IsAssignableFrom(sourceType))
            {
                var conversionResult = ConversionResult.Success(context.Source);
                return new ValueTask<ConversionResult>(conversionResult);
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
    }
}
