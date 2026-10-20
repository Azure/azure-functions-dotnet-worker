// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.AspNetMiddleware;
using Microsoft.Azure.Functions.Worker.Testing;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Testing;

internal sealed class FunctionsTestingDispatchMiddleware : IFunctionsHttpRequestDispatcher
{
    private readonly IFunctionsTestInvocationDispatcher _dispatcher;

    public FunctionsTestingDispatchMiddleware(
        IFunctionsTestInvocationDispatcher dispatcher,
        IHttpCoordinator coordinator)
    {
        _dispatcher = dispatcher;
        _ = coordinator;
    }

    public async Task DispatchAsync(HttpContext context, RequestDelegate next)
    {
        Endpoint? endpoint = context.GetEndpoint();
        FunctionEndpointMetadata? function = endpoint?.Metadata.GetMetadata<FunctionEndpointMetadata>();
        if (function is null)
        {
            if (endpoint?.RequestDelegate is { } requestDelegate)
            {
                await requestDelegate(context);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
            }

            return;
        }

        string functionName = function.FunctionName;
        string invocationId = Guid.NewGuid().ToString("N");
        context.Request.Headers[Constants.CorrelationHeader] = invocationId;
        FunctionsTestHttpRequest request = await CreateRequestAsync(context.Request, context.RequestAborted);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        Task<FunctionInvocationResult> invocation = _dispatcher.InvokeHttpAsync(
            functionName,
            request,
            invocationId,
            cancellation.Token);

        try
        {
            await next(context);
            FunctionInvocationResult result = await invocation;
            if (result.Status == FunctionInvocationStatus.Failed)
            {
                throw new InvalidOperationException(
                    $"ASP.NET Core function '{functionName}' failed: "
                    + (result.Exception is null
                        ? "The worker returned no exception details."
                        : $"{result.Exception.Message}{Environment.NewLine}{result.Exception.StackTrace}"));
            }
        }
        catch
        {
            cancellation.Cancel();
            try
            {
                await invocation;
            }
            catch
            {
            }

            throw;
        }
    }

    private static async Task<FunctionsTestHttpRequest> CreateRequestAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        request.EnableBuffering();
        using var body = new MemoryStream();
        await request.Body.CopyToAsync(body, cancellationToken);
        request.Body.Position = 0;
        if (request.ContentLength is null && body.Length > 0)
        {
            request.ContentLength = body.Length;
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach ((string name, Microsoft.Extensions.Primitives.StringValues values) in request.Headers)
        {
            headers[name] = values.ToString();
        }

        var url = new Uri(
            $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}");
        return new FunctionsTestHttpRequest(
            request.Method,
            url,
            headers,
            body.ToArray());
    }
}
