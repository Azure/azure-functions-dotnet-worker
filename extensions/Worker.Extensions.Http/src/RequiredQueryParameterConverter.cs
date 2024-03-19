// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.Converters
{
    internal class RequiredQueryParameterConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (context.TargetType == typeof(string) && string.IsNullOrWhiteSpace(context.Source?.ToString()))
            {
                throw new InvalidOperationException("Required query parameter is missing");
            }

            return new ValueTask<ConversionResult>(ConversionResult.Success(context.Source!));
        }
    }
}
