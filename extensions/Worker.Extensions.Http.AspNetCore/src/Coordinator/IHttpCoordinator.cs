// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Coordinator
{
    internal interface IHttpCoordinator
    {
        public Task<HttpContext> SetFunctionContextAsync(string invocationId, FunctionContext context);

        public Task<FunctionContext> SetHttpContextAsync(string invocationId, HttpContext context);

        public Task RunFunctionInvocationAsync(string invocationId);

        public void CompleteFunctionInvocation(string invocationId);
    }
}
