// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Worker.Converters
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
                // look through Items to find the HttpContext and then return the request
                if (context.FunctionContext.Items.TryGetValue("HttpRequestContext", out object request) 
                    && request is HttpContext httpContext)
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
