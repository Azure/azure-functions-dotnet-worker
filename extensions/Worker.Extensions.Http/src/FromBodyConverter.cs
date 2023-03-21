// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.Converters
{
    internal class FromBodyConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            var bodyConversionFeature = context.FunctionContext.Features.Get<IFromBodyConversionFeature>()
                 ?? DefaultFromBodyConversionFeature.Instance;

            ValueTask<object?> result = bodyConversionFeature.ConvertAsync(context.FunctionContext, context.TargetType);

            if (result.IsCompletedSuccessfully)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Success(result.Result));
            }

            return HandleResultAsync(result);
        }

        private async ValueTask<ConversionResult> HandleResultAsync(ValueTask<object?> result)
        {
            object? bodyResult = await result;

            return ConversionResult.Success(bodyResult);
        }
    }
}
