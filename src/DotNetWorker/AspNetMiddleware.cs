using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Functions.Worker.Core.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Functions.Worker.Core.Pipeline
{
    internal class AspNetMiddleware
    {
        private IHttpCoordinator _coordinator;
        private readonly RequestDelegate _requestDelegate;

        public AspNetMiddleware(RequestDelegate next, IHttpCoordinator httpCoordinator) 
        {
            _requestDelegate = next;
            _coordinator = httpCoordinator;
        }

        // want this to be end of pipeline
        public async Task Invoke(HttpContext context)
        {
            context.Request.Headers.TryGetValue("invocation-id", out StringValues invocationId);

            // handle null invocation ids later
            await _coordinator.SetContextAsync(invocationId.FirstOrDefault()!, context);
        }
    }

    /// <summary>
    /// Custom ASP.NET Core Middleware Extensions
    /// </summary>
    public static class CustomMiddlewareExtension
    {
        /// <summary>
        /// Use the AspNetMiddleware for forwarding HTTP requests to be used by the coordinator service.
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
