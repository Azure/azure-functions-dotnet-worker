// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// Converter to bind DateTime/DateTime?/DateOnly/TimeOnly type parameters.
    /// </summary>
    internal sealed class DateTimeConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (
                context.TargetType == typeof(DateTime) || context.TargetType == typeof(DateTime?)
#if NET6_0
                || context.TargetType == typeof(DateOnly) || context.TargetType == typeof(DateOnly?)
                || context.TargetType == typeof(TimeOnly) || context.TargetType == typeof(TimeOnly?)
#endif
                )
            {
#if NET6_0
                if (context.Source is string source
                    && (context.TargetType == typeof(DateOnly) || context.TargetType == typeof(DateOnly?))
                    && DateOnly.TryParse(source, out DateOnly parsedDateOnly))
                {
                    return new ValueTask<ConversionResult>(ConversionResult.Success(parsedDateOnly));
                }
                if (context.Source is string sourceStr
                    && (context.TargetType == typeof(TimeOnly) || context.TargetType == typeof(TimeOnly?))
                    && TimeOnly.TryParse(sourceStr, out TimeOnly parsedTimeOnly))
                {
                    return new ValueTask<ConversionResult>(ConversionResult.Success(parsedTimeOnly));
                }
#endif
                if (context.Source is string sourceString && DateTime.TryParse(sourceString, out DateTime parsedDate))
                {
                    return new ValueTask<ConversionResult>(ConversionResult.Success(parsedDate));
                }
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
    }
}
