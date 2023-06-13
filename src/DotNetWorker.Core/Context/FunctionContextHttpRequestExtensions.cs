﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// FunctionContext extensions for http trigger function invocations.
    /// </summary>
    public static class FunctionContextHttpRequestExtensions
    {
        private const string HttpBindingType = "http";

        /// <summary>
        /// Gets the <see cref="HttpRequestData"/> instance if the invocation is for an http trigger.
        /// </summary>
        /// <param name="context">The FunctionContext instance.</param>
        /// <returns>HttpRequestData instance if the invocation is http, else null</returns>
        public static async ValueTask<HttpRequestData?> GetHttpRequestDataAsync(this FunctionContext context)
        {
            IHttpRequestDataFeature? httpRequestDataFeature = context.Features.Get<IHttpRequestDataFeature>() ?? DefaultHttpRequestDataFeature.Instance;

            return await httpRequestDataFeature.GetHttpRequestDataAsync(context);
        }

        /// <summary>
        /// Gets the <see cref="HttpResponseData"/> instance if the invocation is for an http trigger.
        /// </summary>
        /// <param name="context">The FunctionContext instance.</param>
        /// <returns>HttpResponseData instance if the invocation is http, else null</returns>
        public static HttpResponseData? GetHttpResponseData(this FunctionContext context)
        {
            var httpInvocationResult = context.GetInvocationResult();
            if (httpInvocationResult.Value is HttpResponseData responseData)
            {
                return responseData;
            }

            // see output binding entries have a property of type HttpResponseData;
            var httpOutputBinding = context.GetOutputBindings<HttpResponseData>().FirstOrDefault(a => string.Equals(a.BindingType, HttpBindingType, StringComparison.OrdinalIgnoreCase));

            return httpOutputBinding?.Value;
        }
    }
}
