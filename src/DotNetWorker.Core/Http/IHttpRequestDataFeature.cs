// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Http
{
    /// <summary>
    /// A representation of the HTTP request sent by the host
    /// </summary>
    public interface IHttpRequestDataFeature
    {
        /// <summary>
        /// /// <summary>
        /// Gets the <see cref="HttpRequestData"/> instance if the FunctionContext contains an invocation for an http trigger.
        /// </summary>
        /// <param name="context">The FunctionContext instance.</param>
        /// <returns>HttpRequestData instance if the invocation is http, else null</returns>
        /// </summary>
        ValueTask<HttpRequestData?> GetHttpRequestDataAsync(FunctionContext context);
    }
}
