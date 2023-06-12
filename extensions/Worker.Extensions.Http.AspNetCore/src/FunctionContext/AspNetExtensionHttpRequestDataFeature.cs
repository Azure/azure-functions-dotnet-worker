// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal class AspNetExtensionHttpRequestDataFeature : IHttpRequestDataFeature
    {
        private AspNetExtensionHttpRequestDataFeature()
        {
        }

        public static AspNetExtensionHttpRequestDataFeature Instance { get; } = new AspNetExtensionHttpRequestDataFeature();

        public ValueTask<HttpRequestData?> GetHttpRequestDataAsync(FunctionContext context)
        {
            throw new NotSupportedException($"The method {nameof(GetHttpRequestDataAsync)} " +
                $"is not supported when using the ASP.NET Core integration. Use the GetHttpContext method of {nameof(FunctionContext)} to access the HttpContext for the request.");
        }
    }
}
