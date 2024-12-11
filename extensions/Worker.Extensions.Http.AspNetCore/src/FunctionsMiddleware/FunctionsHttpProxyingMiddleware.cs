// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Infrastructure;
using Microsoft.Azure.Functions.Worker.Extensions.Http.Converters;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal class FunctionsHttpProxyingMiddleware : IFunctionsWorkerMiddleware
    {
        private const string HttpTrigger = "httpTrigger";
        private const string HttpBindingType = "http";

        private readonly IHttpCoordinator _coordinator;
        private readonly ConcurrentDictionary<string, bool> _isHttpTrigger = new();

        public FunctionsHttpProxyingMiddleware(IHttpCoordinator httpCoordinator)
        {
            _coordinator = httpCoordinator;
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            // Only use the coordinator for HttpTriggers
            if (!_isHttpTrigger.GetOrAdd(context.FunctionId, static (_, c) => IsHttpTriggerFunction(c), context))
            {
                await next(context);
                return;
            }

            var invocationId = context.InvocationId;

            // this call will block until the ASP.NET middleware pipeline has signaled that it's ready to run the function
            var httpContext = await _coordinator.SetFunctionContextAsync(invocationId, context);

            AddHttpContextToFunctionContext(context, httpContext);

            // Register additional context features
            context.Features.Set<IFromBodyConversionFeature>(FromBodyConversionFeature.Instance);

            try
            {
                await next(context);

                var responseHandled = await TryHandleHttpResult(context.GetInvocationResult().Value, context, httpContext, true)
                    || await TryHandleOutputBindingsHttpResult(context, httpContext);

                if (!responseHandled)
                {
                    var logger = context.InstanceServices.GetRequiredService<ExtensionTrace>();
                    logger.NoHttpResponseReturned(context.FunctionDefinition.Name, context.InvocationId);
                }
            }
            finally
            {
                // Allow ASP.NET Core middleware to continue
                _coordinator.CompleteFunctionInvocation(invocationId);
            }
        }

        private static async Task<bool> TryHandleHttpResult(object? result, FunctionContext context, HttpContext httpContext, bool isInvocationResult = false)
        {
            switch (result)
            {
                case IActionResult actionResult:
                    ActionContext actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());
                    await actionResult.ExecuteResultAsync(actionContext);
                    break;
                case AspNetCoreHttpResponseData when isInvocationResult:
                    // The AspNetCoreHttpResponseData implementation is
                    // simply a wrapper over the underlying HttpResponse and
                    // all APIs manipulate the request.
                    // There's no need to return this result as no additional
                    // processing is required.
                    context.GetInvocationResult().Value = null;
                    break;
                case AspNetCoreHttpResponseData when !isInvocationResult:
                    await TryClearHttpOutputBinding(context);
                    break;
                case IResult iResult:
                    await iResult.ExecuteAsync(httpContext);
                    break;
                default:
                    return false;
            }

            return true;
        }

        private static Task<bool> TryHandleOutputBindingsHttpResult(FunctionContext context, HttpContext httpContext)
        {
            var httpOutputBinding = context.GetOutputBindings<object>()
                .FirstOrDefault(a => string.Equals(a.BindingType, HttpBindingType, StringComparison.OrdinalIgnoreCase));

            return httpOutputBinding is null
                ? Task.FromResult(false)
                : TryHandleHttpResult(httpOutputBinding.Value, context, httpContext);
        }

        private static Task<bool> TryClearHttpOutputBinding(FunctionContext context) 
        {
            var httpOutputBinding = context.GetOutputBindings<object>()
                .FirstOrDefault(a => string.Equals(a.BindingType, HttpBindingType, StringComparison.OrdinalIgnoreCase));

            if (httpOutputBinding is null)
            {
                return Task.FromResult(false);
            }

            httpOutputBinding.Value = null;

            return Task.FromResult(true);
        }

        private static void AddHttpContextToFunctionContext(FunctionContext funcContext, HttpContext httpContext)
        {
            funcContext.Items.Add(Constants.HttpContextKey, httpContext);

            // Add ASP.NET Core integration version of IHttpRequestDataFeature
            funcContext.Features.Set<IHttpRequestDataFeature>(AspNetCoreHttpRequestDataFeature.Instance);
        }

        private static bool IsHttpTriggerFunction(FunctionContext funcContext)
        {
            return funcContext.FunctionDefinition.InputBindings
                .Any(p => p.Value.Type.Equals(HttpTrigger, StringComparison.OrdinalIgnoreCase));
        }
    }
}
