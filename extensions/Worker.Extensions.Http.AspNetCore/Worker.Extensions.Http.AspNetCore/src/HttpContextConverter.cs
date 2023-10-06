// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    /// <summary>
    /// Converter to bind HttpRequest type parameters.
    /// </summary>
    internal class HttpContextConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            object? target = null;

            if (context.TargetType == typeof(HttpRequest))
            {
                if (context.FunctionContext.Items.TryGetValue(Constants.HttpContextKey, out var requestContext)
                    && requestContext is HttpContext httpContext)
                {
                    target = httpContext.Request;
                }
            }

            if (target is not null)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Success(target));
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
    }
}
