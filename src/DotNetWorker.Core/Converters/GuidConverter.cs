// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// Converter to bind Guid/Guid? type parameters.
    /// </summary>
    internal class GuidConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (context.TargetType == typeof(Guid) || context.TargetType == typeof(Guid?))
            {
                if (context.Source is string sourceString && Guid.TryParse(sourceString, out Guid parsedGuid))
                {
                    return new ValueTask<ConversionResult>(ConversionResult.Success(parsedGuid));
                }
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
    }
}
