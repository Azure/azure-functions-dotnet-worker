using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;

internal class WorkerRequestServicesMiddleware
{
    private readonly IHttpCoordinator _coordinator;
    private readonly RequestDelegate _next;

    public WorkerRequestServicesMiddleware(RequestDelegate next, IHttpCoordinator httpCoordinator)
    {
        _next = next;
        _coordinator = httpCoordinator;
    }

    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(Constants.CorrelationHeader, out StringValues invocationId)
            || invocationId == 0 || invocationId.Count == 0)
        {
            throw new InvalidOperationException($"Expected correlation id header ('{Constants.CorrelationHeader}') not present");
        }

        FunctionContext functionContext = await _coordinator.SetHttpContextAsync(invocationId, context);

        // Explicitly set the RequestServices to prevent a new scope from being created internally.
        // This also prevents the scope from being disposed when the request is complete. We want this to 
        // be disposed in the Functions middleware, not here.
        var servicesFeature = new RequestServicesFeature(context, null)
        {
            RequestServices = functionContext.InstanceServices
        };
        context.Features.Set<IServiceProvidersFeature>(servicesFeature);

        await _next(context);
    }
}
