// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Coordinator;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.FunctionsMiddleware
{
    internal class FunctionsHttpProxyingMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly IHttpCoordinator _coordinator;

        public FunctionsHttpProxyingMiddleware(IHttpCoordinator httpCoordinator)
        {
            _coordinator = httpCoordinator;
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var invocationId = context.InvocationId;

            // this call will block until the ASP.NET middleware pipleline has signaled that it's ready to run the function
            var httpContext = await _coordinator.SetFunctionContextAsync(invocationId, context);

            AddHttpContextToFunctionContext(context, httpContext);

            await next(context);

            // allows asp.net middleware to continue
            _coordinator.CompleteFunctionInvocation(invocationId);
        }

        private static void AddHttpContextToFunctionContext(FunctionContext funcContext, HttpContext httpContext)
        {
            funcContext.Items.Add("HttpRequestContext", httpContext);
        }
    }
}
