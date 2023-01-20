// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Core.Http
{
    internal interface IHttpCoordinator
    {
        public Task SetContextAsync(string functionId, HttpContext context);

        public Task<HttpContext> GetContextAsync(string invocationId);

        public void CompleteInvocation(string invocationId);
    }
}
