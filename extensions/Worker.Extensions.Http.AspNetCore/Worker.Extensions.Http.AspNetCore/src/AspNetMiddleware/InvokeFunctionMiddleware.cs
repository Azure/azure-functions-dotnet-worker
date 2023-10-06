// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal class InvokeFunctionMiddleware
    {
        public InvokeFunctionMiddleware(RequestDelegate next)
        {
        }

        public Task Invoke(HttpContext context)
        {
            return context.InvokeFunctionAsync();
        }
    }
}
