// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Core.Http;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Microsoft.Azure.Functions.Worker.Pipeline
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

            var httpContext = await _coordinator.GetContextAsync(invocationId);

            AddHttpContextToFunctionContext(context, httpContext);

            await next(context);

            // allows asp.net middleware to continue
            _coordinator.CompleteInvocation(invocationId);
        }

        private void AddHttpContextToFunctionContext(FunctionContext funcContext, HttpContext httpContext)
        {
            funcContext.Items.Add("HttpRequestContext", httpContext); 
        }
    }
}
