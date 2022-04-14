// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// Converter to bind to DateTime/DateTimeOffset/DateOnly/TimeOnly types.
    /// </summary>
    internal sealed class DateTimeConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (!IsValidTargetType(context) || context.Source is not string source)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
            }

            if ((context.TargetType == typeof(DateTimeOffset) || context.TargetType == typeof(DateTimeOffset?))
                && DateTimeOffset.TryParse(source, out var parsedDateTimeOffset))
            {
                return new ValueTask<ConversionResult>(ConversionResult.Success(parsedDateTimeOffset));
            }
#if NET6_0
            if ((context.TargetType == typeof(DateOnly) || context.TargetType == typeof(DateOnly?))
                && DateOnly.TryParse(source, out var parsedDateOnly))
            {
                return new ValueTask<ConversionResult>(ConversionResult.Success(parsedDateOnly));
            }

            if ((context.TargetType == typeof(TimeOnly) || context.TargetType == typeof(TimeOnly?))
                && TimeOnly.TryParse(source, out var parsedTimeOnly))
            {
                return new ValueTask<ConversionResult>(ConversionResult.Success(parsedTimeOnly));
            }
#endif  

            if (DateTime.TryParse(source, out DateTime parsedDate))
            {
                return new ValueTask<ConversionResult>(ConversionResult.Success(parsedDate));
            }
            
            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }

        private static bool IsValidTargetType(ConverterContext context)
        {
            if (context.TargetType == typeof(DateTime)
                || context.TargetType == typeof(DateTime?)
                || context.TargetType == typeof(DateTimeOffset)
                || context.TargetType == typeof(DateTimeOffset?)
#if NET6_0
                || context.TargetType == typeof(DateOnly)
                || context.TargetType == typeof(DateOnly?)
                || context.TargetType == typeof(TimeOnly)
                || context.TargetType == typeof(TimeOnly?)
#endif
              )
            {
                return true;
            }

            return false;
        }
    }
}
