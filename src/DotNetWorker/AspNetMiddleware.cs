// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Core.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Functions.Worker.Core.Pipeline
{
    internal class AspNetMiddleware
    {
        private IHttpCoordinator _coordinator;
        private readonly RequestDelegate _next;

        public AspNetMiddleware(RequestDelegate next, IHttpCoordinator httpCoordinator) 
        {
            _next = next;
            _coordinator = httpCoordinator;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Request.Headers.TryGetValue("invocation-id", out StringValues invocationId);

            if (invocationId == 0 || invocationId.Count == 0) 
            {
                throw new ArgumentNullException("Some message?");
            }

            // TODO: Discuss whether we need to handle invocationId (a StringValues obj) being more than one string? 
            // Likely not since this info is sent from host which we control?
            await _coordinator.SetContextAsync(invocationId, context);
        }
    }

    /// <summary>
    /// Custom ASP.NET Core Middleware Extensions
    /// </summary>
    public static class CustomMiddlewareExtension
    {
        /// <summary>
        /// Use the AspNetHttpForwarderMiddleware to forward an HttpRequest to the Functions middleware pipeline through the use of a coordinator service.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseAspNetHttpForwarderMiddleware(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AspNetMiddleware>();
        }
    }
}
