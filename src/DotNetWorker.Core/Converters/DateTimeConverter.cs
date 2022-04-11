// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// Converter to bind DateTime/DateTime? type parameters.
    /// </summary>
    internal sealed class DateTimeConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (context.TargetType == typeof(DateTime) || context.TargetType == typeof(DateTime?))
            {
                if (context.Source is string sourceString && DateTime.TryParse(sourceString, out DateTime parsedDate))
                {
                    return new ValueTask<ConversionResult>(ConversionResult.Success(parsedDate));
                }
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
    }
}
