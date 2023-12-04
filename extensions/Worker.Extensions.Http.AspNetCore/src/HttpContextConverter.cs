// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Http;

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

            if (context.TargetType == typeof(HttpRequest)
                && context.FunctionContext.TryGetRequest(out var request))
            {
                target = request;
            }
            else if (context.TargetType == typeof(HttpRequestData)
                && context.FunctionContext.TryGetRequest(out request))
            {
                target = new AspNetCoreHttpRequestData(request, context.FunctionContext);
            }
            
            if (target is not null)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Success(target));
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
    }
}
