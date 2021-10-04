// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class FunctionContextConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            // Special handling for the context.
            if (context.TargetType == typeof(FunctionContext))
            {
                return new ValueTask<ConversionResult>(ConversionResult.Success(context.FunctionContext));
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
    }
}
