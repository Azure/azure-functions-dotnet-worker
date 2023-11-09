// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal class AspNetCoreHttpRequestDataFeature : IHttpRequestDataFeature
    {
        private AspNetCoreHttpRequestDataFeature()
        {
        }

        /// <summary>
        /// Gets the singleton instance of the <see cref="AspNetCoreHttpRequestDataFeature"/> class.
        /// </summary>
        public static AspNetCoreHttpRequestDataFeature Instance { get; } = new AspNetCoreHttpRequestDataFeature();

        /// <inheritdoc/>
        public ValueTask<HttpRequestData?> GetHttpRequestDataAsync(FunctionContext context)
            => context.TryGetRequest(out var request) ? new(new AspNetCoreHttpRequestData(request, context)) : default;
    }
}
