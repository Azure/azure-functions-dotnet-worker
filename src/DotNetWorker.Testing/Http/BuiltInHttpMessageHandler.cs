// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Testing.Http;

internal sealed class BuiltInHttpMessageHandler<TEntryPoint> : HttpMessageHandler
    where TEntryPoint : class
{
    private readonly FunctionsApplicationFactory<TEntryPoint> _factory;
    private readonly string _functionName;
    private readonly string _triggerName;

    internal BuiltInHttpMessageHandler(
        FunctionsApplicationFactory<TEntryPoint> factory,
        string functionName,
        string triggerName)
    {
        _factory = factory;
        _functionName = functionName;
        _triggerName = triggerName;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Uri url = request.RequestUri
            ?? throw new InvalidOperationException("The HTTP request must have a request URI.");
        var rpcHttp = new RpcHttp
        {
            Method = request.Method.Method,
            Url = url.AbsoluteUri
        };

        foreach ((string name, string value) in request.Headers
                     .Select(header => new KeyValuePair<string, string>(header.Key, string.Join(",", header.Value))))
        {
            rpcHttp.Headers[name] = value;
            rpcHttp.NullableHeaders[name] = new NullableString { Value = value };
        }

        if (request.Content is not null)
        {
            foreach ((string name, string value) in request.Content.Headers
                         .Select(header => new KeyValuePair<string, string>(header.Key, string.Join(",", header.Value))))
            {
                rpcHttp.Headers[name] = value;
                rpcHttp.NullableHeaders[name] = new NullableString { Value = value };
            }
        }

        if (request.Content is not null)
        {
            byte[] content = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            rpcHttp.Body = new TypedData { Bytes = ByteString.CopyFrom(content) };
            rpcHttp.RawBody = new TypedData { Bytes = ByteString.CopyFrom(content) };
        }
        else
        {
            rpcHttp.Body = new TypedData { Bytes = ByteString.Empty };
            rpcHttp.RawBody = new TypedData { Bytes = ByteString.Empty };
        }

        PopulateQuery(url, rpcHttp);
        InvocationResponse response = await _factory.InvokeHttpAsync(
            _functionName,
            _triggerName,
            rpcHttp,
            cancellationToken);

        if (response.Result?.Status != StatusResult.Types.Status.Success)
        {
            string message = response.Result?.Exception?.Message
                ?? response.Result?.Result
                ?? "The function failed without exception details.";
            throw new HttpRequestException(
                $"Function '{_functionName}' completed with status '{response.Result?.Status}': {message}");
        }

        if (response.ReturnValue?.DataCase != TypedData.DataOneofCase.Http)
        {
            throw new InvalidOperationException(
                $"Function '{_functionName}' did not return HttpResponseData. "
                + "CreateHttpClient(functionName) requires an HTTP protocol response.");
        }

        return ToHttpResponse(response.ReturnValue.Http);
    }

    private static void PopulateQuery(Uri url, RpcHttp rpcHttp)
    {
        string query = url.Query.TrimStart('?');
        if (query.Length == 0)
        {
            return;
        }

        foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=', 2);
            string name = Uri.UnescapeDataString(parts[0].Replace("+", " ", StringComparison.Ordinal));
            string value = parts.Length == 1
                ? string.Empty
                : Uri.UnescapeDataString(parts[1].Replace("+", " ", StringComparison.Ordinal));
            rpcHttp.Query[name] = value;
            rpcHttp.NullableQuery[name] = new NullableString { Value = value };
        }
    }

    private static HttpResponseMessage ToHttpResponse(RpcHttp rpc)
    {
        if (!int.TryParse(rpc.StatusCode, NumberStyles.None, CultureInfo.InvariantCulture, out int statusCode))
        {
            throw new InvalidOperationException($"The worker returned invalid HTTP status code '{rpc.StatusCode}'.");
        }

        var response = new HttpResponseMessage((HttpStatusCode)statusCode)
        {
            Content = ToHttpContent(rpc.Body)
        };

        foreach ((string name, string value) in rpc.Headers)
        {
            if (!response.Headers.TryAddWithoutValidation(name, value))
            {
                response.Content.Headers.TryAddWithoutValidation(name, value);
            }
        }

        return response;
    }

    private static HttpContent ToHttpContent(TypedData? body)
        => body?.DataCase switch
        {
            null or TypedData.DataOneofCase.None => new ByteArrayContent(Array.Empty<byte>()),
            TypedData.DataOneofCase.Bytes => new ByteArrayContent(body.Bytes.ToByteArray()),
            TypedData.DataOneofCase.String => new StringContent(body.String),
            TypedData.DataOneofCase.Json => new StringContent(body.Json),
            _ => throw new NotSupportedException($"HTTP response body kind '{body.DataCase}' is not supported.")
        };
}
