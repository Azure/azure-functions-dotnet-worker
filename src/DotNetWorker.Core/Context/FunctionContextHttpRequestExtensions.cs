// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.Functions.Worker.Core.Context
{
    /// <summary>
    /// FunctionContext extensions for http trigger function invocations.
    /// </summary>
    public static class FunctionContextHttpRequestExtensions
    {
        /// <summary>
        /// Gets the <see cref="HttpRequestData"/> instance if the invocation is for an http trigger.
        /// </summary>
        /// <param name="context">The FunctionContext instance.</param>
        /// <returns>HttpRequestData instance if the invocation is http, else null</returns>
        public static async Task<HttpRequestData?> GetHttpRequestData(this FunctionContext context)
        {
            return await context.BindInputAsync<HttpRequestData>();
        }
    }
}
