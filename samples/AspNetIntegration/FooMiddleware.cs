using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace AspNetIntegration
{
    internal class FooMiddleware : IFunctionsWorkerMiddleware
    {
        public Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            // retrieve http context from function context
            HttpContext httpContext = context.GetHttpContext() 
                ?? throw new InvalidOperationException($"{nameof(context)} has no http context associated with it.");

            // operations can be performed using HttpContext here

            // continue along with function execution
            return next(context);
        }
    }
}
