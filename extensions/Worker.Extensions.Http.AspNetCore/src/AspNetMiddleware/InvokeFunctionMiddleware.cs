// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNet;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Coordinator;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Functions.Worker.Core.Pipeline
{
    internal class InvokeFunctionMiddleware
    {
        private readonly IHttpCoordinator _coordinator;

        public InvokeFunctionMiddleware(RequestDelegate next, IHttpCoordinator httpCoordinator)
        {
            _coordinator = httpCoordinator;
        }

        public Task Invoke(HttpContext context)
        {
            context.Request.Headers.TryGetValue(Constants.CorrelationHeader, out StringValues invocationId);
            return _coordinator.RunFunctionInvocationAsync(invocationId);
        }
    }
}
