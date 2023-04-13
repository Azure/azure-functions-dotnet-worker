using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNet;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Coordinator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;

internal class SetServiceProviderMiddleware
{
    private IHttpCoordinator _coordinator;
    private readonly RequestDelegate _next;

    public SetServiceProviderMiddleware(RequestDelegate next, IHttpCoordinator httpCoordinator)
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
        context.Features.Set<IServiceProvidersFeature>(new RequestServicesFeature(context, functionContext.InstanceServices as IServiceScopeFactory));

        await _next(context);
    }
}
