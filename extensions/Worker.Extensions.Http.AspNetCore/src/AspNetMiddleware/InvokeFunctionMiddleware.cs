// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal class InvokeFunctionMiddleware
    {
        private readonly IHttpCoordinator _coordinator;

        public InvokeFunctionMiddleware(RequestDelegate next, IHttpCoordinator httpCoordinator)
        {
            _coordinator = httpCoordinator;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Request.Headers.TryGetValue(Constants.CorrelationHeader, out StringValues invocationId);
            InvocationResult invocationResult = await _coordinator.RunFunctionInvocationAsync(invocationId);

/*            if (invocationResult.Value is IActionResult actionResult)
            {
                ActionContext actionContext = new ActionContext(context, context.GetRouteData(), new ActionDescriptor());

                await actionResult.ExecuteResultAsync(actionContext);
            }*/
        }
    }
}
