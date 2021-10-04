// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class TypeConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            Type? sourceType = context.Source?.GetType();

            if (sourceType is not null &&
                context.TargetType.IsAssignableFrom(sourceType))
            {
                var bindingResult = ConversionResult.Success(context.Source);
                return new ValueTask<ConversionResult>(bindingResult);
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
    }
}
