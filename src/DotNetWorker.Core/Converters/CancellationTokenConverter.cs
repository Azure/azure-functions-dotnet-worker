// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class CancellationTokenConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (context.TargetType == typeof(CancellationToken) || context.TargetType == typeof(CancellationToken?))
            {
                return new ValueTask<ConversionResult>(ConversionResult.Success(context.FunctionContext.CancellationToken));
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
    }
}
