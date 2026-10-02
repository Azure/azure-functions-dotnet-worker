// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AspNetCoreFunctionApp;

public sealed class TestFunctions(AppMarker marker)
{
    [Function("AspNetOrder")]
    public IActionResult Order(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/{id:int}")] HttpRequest request,
        [Microsoft.Azure.Functions.Worker.Http.FromBody] OrderRequest body,
        FunctionContext context)
        => new ObjectResult(new
        {
            Id = request?.RouteValues["id"]?.ToString() ?? "<null-request>",
            Name = body?.Name ?? "<null-body>",
            Marker = marker?.Value ?? "<null-service>",
            Middleware = context?.Items["fixture-middleware"]?.ToString() ?? "<null-context>"
        })
        {
            StatusCode = StatusCodes.Status201Created
        };

    [Function("AspNetResult")]
    public IResult Result(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "results/{value}")] HttpRequest request)
        => Results.Json(new { Value = request.RouteValues["value"]?.ToString() });

    [Function("AspNetFail")]
    public IActionResult Fail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "fail")] HttpRequest request)
        => throw new InvalidOperationException("ASP.NET fixture failure.");

    [Function("AspNetCancel")]
    public async Task<IResult> Cancel(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cancel")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return Results.Ok();
    }

    [Function("AspNetCookieSet")]
    public IResult CookieSet(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cookies/set")] HttpRequest request)
    {
        request.HttpContext.Response.Cookies.Append("fixture-cookie", "cookie-value");
        return Results.Redirect("/api/cookies/read");
    }

    [Function("AspNetCookieRead")]
    public IResult CookieRead(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cookies/read")] HttpRequest request)
        => Results.Text(request.Cookies["fixture-cookie"] ?? "missing");
}

public sealed class OrderRequest
{
    public string? Name { get; set; }
}
