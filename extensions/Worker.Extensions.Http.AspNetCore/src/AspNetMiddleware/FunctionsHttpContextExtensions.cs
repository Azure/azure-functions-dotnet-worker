// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http
{
    internal static class FunctionsHttpContextExtensions
    {
        internal static Task InvokeFunctionAsync(this HttpContext context)
        {
            var coordinator = context.RequestServices.GetRequiredService<IHttpCoordinator>();
            context.Request.Headers.TryGetValue(Constants.CorrelationHeader, out StringValues invocationId);
            return coordinator.RunFunctionInvocationAsync(invocationId!); // will fail later if null.
        }
    }
}
