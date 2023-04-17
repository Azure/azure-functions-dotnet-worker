// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal interface IHttpCoordinator
    {
        /// <summary>
        /// Sets the FunctionContext for the specified invocation. 
        /// This will block until the ASP.NET middleware pipeline has signaled that it's ready to run the function.
        /// </summary>
        /// <param name="invocationId">The invocation id.</param>
        /// <param name="context">The context.</param>
        /// <returns>A Task that completes when the ASP.NET middleware has signaled that the Function middleware can continue.</returns>
        public Task<HttpContext> SetFunctionContextAsync(string invocationId, FunctionContext context);

        /// <summary>
        /// Sets the HttpContext for the specified invocation.
        /// </summary>
        /// <param name="invocationId">The invocation id.</param>
        /// <param name="context">The context.</param>
        /// <returns>A Task that completes when the FunctionContext is available.</returns>
        public Task<FunctionContext> SetHttpContextAsync(string invocationId, HttpContext context);

        /// <summary>
        /// Signals the Functions middleware pipeline that it can continue with the specified invocation.
        /// </summary>
        /// <param name="invocationId">The invocation id.</param>
        /// <returns>A Task that completes when the function invocation is complete.</returns>
        public Task<InvocationResult> RunFunctionInvocationAsync(string invocationId);

        /// <summary>
        /// Signals that the function invocation is complete. Allows the ASP.NET middleware pipeline to continue.
        /// </summary>
        /// <param name="invocationId">A Task that completes when the function invocation is complete.</param>
        /// <param name="functionContext"></param>
        public void CompleteFunctionInvocation(string invocationId, FunctionContext functionContext);
    }
}
