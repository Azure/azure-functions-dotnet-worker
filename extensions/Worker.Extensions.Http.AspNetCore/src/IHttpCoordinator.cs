// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Core.Http
{
    internal interface IHttpCoordinator
    {
        public Task<FunctionContext> SetContextAsync(string functionId, HttpContext context);

        public Task<HttpContext> GetContextAsync(string invocationId, CancellationToken cancellationToken);

        public void CompleteInvocation(string invocationId, FunctionContext functionContext);
    }
}
