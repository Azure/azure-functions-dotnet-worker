// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Functions.Worker.Extensions.Http.Converters;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal class FunctionsHttpProxyingMiddleware : IFunctionsWorkerMiddleware
    {
        private const string HttpTrigger = "httpTrigger";

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
            context.Features.Set<IFromBodyConversionFeature>(FromBodyConverstionFeature.Instance);

            await next(context);

            if (context.GetInvocationResult()?.Value is IActionResult actionResult)
            {
                ActionContext actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());

                await actionResult.ExecuteResultAsync(actionContext);
            }

            // allows asp.net middleware to continue
            _coordinator.CompleteFunctionInvocation(invocationId);
        }

        private static void AddHttpContextToFunctionContext(FunctionContext funcContext, HttpContext httpContext)
        {
            funcContext.Items.Add(Constants.HttpContextKey, httpContext);

            // add asp net version of httprequestdata feature
            funcContext.Features.Set<IHttpRequestDataFeature>(AspNetCoreHttpRequestDataFeature.Instance);
        }

        private static bool IsHttpTriggerFunction(FunctionContext funcContext)
        {
            return funcContext.FunctionDefinition.InputBindings
                .Any(p => p.Value.Type.Equals(HttpTrigger, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
