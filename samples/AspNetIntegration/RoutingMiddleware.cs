// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace AspNetIntegration
{
    internal class RoutingMiddleware : IFunctionsWorkerMiddleware
    {
        public Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            // retrieve http context from function context
            HttpContext httpContext = context.GetHttpContext() 
                ?? throw new InvalidOperationException($"{nameof(context)} has no http context associated with it.");

            // operations can be performed using HttpContext
            // example of getting route information from HttpContext:
            if (httpContext.GetEndpoint() is RouteEndpoint endpoint)
            {
                string? displayName = endpoint.DisplayName;
                string? routePattern = endpoint.RoutePattern.RawText;
                IReadOnlyList<string>? httpMethods = (endpoint.Metadata.Single() as HttpMethodMetadata)?.HttpMethods;
            }

            // continue along with function execution
            return next(context);
        }
    }
}
