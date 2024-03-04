// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Provides extension methods to work with <see cref="FunctionContext"/>.
    /// </summary>
    public static class FunctionContextExtensions
    {
        /// <summary>
        /// Retrieve the <see cref="HttpContext"/> for the function execution.
        /// </summary>
        /// <param name="context">The <see cref="FunctionContext"/>.</param>
        /// <returns>The <see cref="HttpContext"/> for the function execution or null if it does not exist.</returns>
        public static HttpContext? GetHttpContext(this FunctionContext context)
        {
            if (context.Items.TryGetValue(Constants.HttpContextKey, out var requestContext)
                && requestContext is HttpContext httpContext)
            {
                return httpContext;
            }

            return null;
        }

        /// <summary>
        /// Gets the <see cref="HttpRequest"/> for the <see cref="FunctionContext"/>.
        /// </summary>
        /// <param name="context">The <see cref="FunctionContext"/>.</param>
        /// <param name="request">The <see cref="HttpRequest"/> for the context.</param>
        /// <returns></returns>
        internal static bool TryGetRequest(this FunctionContext context, [NotNullWhen(true)] out HttpRequest? request)
        {
            request = null;

            if (context.Items.TryGetValue(Constants.HttpContextKey, out var requestContext)
                && requestContext is HttpContext httpContext)
            {
                request = httpContext.Request;
            }

            return request is not null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryGetHttpResponse<T>(this FunctionContext context, [NotNullWhen(true)] out T? result)
        {
            result = default(T);

            var httpInvocationResult = context.GetInvocationResult();
            if (httpInvocationResult.Value is T invocationResult)
            {
                result = invocationResult;
                return true;
            }

            var httpOutputBinding = context.GetOutputBindings<T>().FirstOrDefault();
            if (httpOutputBinding is not null && httpOutputBinding.Value is not null)
            {
                result = httpOutputBinding.Value;
                return true;
            }

            return false;
        }
    }
}
