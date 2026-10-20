// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.ServiceBus;
using Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ModernFunctionApp;

public sealed class TestFunction(ILogger<TestFunction> logger)
{
    [Function("ModernEcho")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData request)
    {
        HttpResponseData response = request.CreateResponse(HttpStatusCode.Created);
        response.Headers.Add("x-echo-method", request.Method);
        response.Headers.Add("x-echo-path-query", request.Url.PathAndQuery);
        if (request.Headers.TryGetValues("x-test-header", out IEnumerable<string>? values))
        {
            response.Headers.Add("x-echo-header", string.Join(",", values));
        }

        await request.Body.CopyToAsync(response.Body);
        return response;
    }

    [Function("GenericEcho")]
    public string GenericEcho(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] string request)
    {
        logger.LogInformation("GenericEcho received {Length} characters.", request.Length);
        return request;
    }

    [Function("GenericFail")]
    public string GenericFail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] string request)
        => throw new InvalidOperationException($"Rejected '{request}'.");

    [Function("GenericCancel")]
    public async Task<string> GenericCancel(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] string request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return request;
    }

    [Function("GenericRetry")]
    public string GenericRetry(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] string request,
        FunctionContext context)
    {
        RetryContext? retry = context.RetryContext;
        FunctionRetryException? previous = retry?.PreviousException;
        return string.Join(
            "|",
            request,
            retry?.RetryCount.ToString() ?? "null",
            retry?.MaxRetryCount.ToString() ?? "null",
            previous?.Source ?? "null",
            previous?.Type ?? "null",
            previous?.Message ?? "null",
            previous?.StackTrace ?? "null",
            previous?.IsUserException.ToString() ?? "null");
    }

    [Function("GenericTrace")]
    public string GenericTrace(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] string request,
        FunctionContext context)
        => string.Join(
            "|",
            request,
            context.TraceContext.TraceParent,
            context.TraceContext.TraceState,
            context.TraceContext.Attributes["trace-key"],
            context.TraceContext.Baggage["baggage-key"]);

    [Function("GenericOutput")]
    public GenericOutputs GenericOutput(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] string request)
        => new()
        {
            ReturnValue = request,
            Output = $"output:{request}"
        };

    [Function("AuthorizedEcho")]
    public async Task<HttpResponseData> AuthorizedEcho(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData request)
    {
        HttpResponseData response = request.CreateResponse(HttpStatusCode.OK);
        await request.Body.CopyToAsync(response.Body);
        return response;
    }

    [Function("NonHttpFunction")]
    public string NonHttpFunction([TestTrigger] string value) => value;

    [Function("ServiceBusMessage")]
    public string ServiceBusMessage(
        [ServiceBusTrigger("testing")] ServiceBusReceivedMessage message,
        string MessageId,
        long DeliveryCount)
        => $"{message.Body}|{message.MessageId}|{MessageId}|{DeliveryCount}|{message.ApplicationProperties["source"]}";

    [Function("ServiceBusBatch")]
    public string ServiceBusBatch(
        [ServiceBusTrigger("testing", IsBatched = true)] ServiceBusReceivedMessage[] messages)
        => string.Join(",", messages.Select(message => message.MessageId));

    [Function("BlobBytes")]
    public string BlobBytes(
        [BlobTrigger("testing/{name}")] byte[] blob,
        string name)
        => $"{name}|{System.Text.Encoding.UTF8.GetString(blob)}";

    [Function("BlobClientIdentity")]
    public string BlobClientIdentity(
        [BlobTrigger("testing/{name}", Connection = "Storage")] BlobClient blob,
        string name)
        => $"{blob.BlobContainerName}|{blob.Name}|{name}";
}

public sealed class TestTriggerAttribute : TriggerBindingAttribute
{
}

public sealed class TestOutputAttribute : OutputBindingAttribute
{
}

public sealed class GenericOutputs
{
    public string? ReturnValue { get; init; }

    [TestOutput]
    public string? Output { get; init; }
}
