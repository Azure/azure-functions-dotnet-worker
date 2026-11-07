// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Testing;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Testing.Tests;

public class AspNetCoreTestClientHandlerTests
{
    [Fact]
    public async Task SendAsync_DisposesOwnedRedirectRequestWhenReturningFinalResponse()
    {
        var innerHandler = new RecordingHandler((request, requestIndex) =>
            requestIndex == 0
                ? new HttpResponseMessage(HttpStatusCode.TemporaryRedirect)
                {
                    Headers = { Location = new Uri("/final", UriKind.Relative) },
                    RequestMessage = request
                }
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("done", Encoding.UTF8),
                    RequestMessage = request
                });
        using var handler = new AspNetCoreTestClientHandler(
            innerHandler,
            new FunctionsTestClientOptions());
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost/start")
        {
            Content = new StringContent("payload", Encoding.UTF8)
        };

        using HttpResponseMessage response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("done", await response.Content.ReadAsStringAsync());
        Assert.Equal(new Uri("https://localhost/final"), response.RequestMessage!.RequestUri);
        Assert.Equal(2, innerHandler.Requests.Count);
        Assert.Same(request, innerHandler.Requests[0]);
        Assert.NotSame(request, innerHandler.Requests[1]);
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => innerHandler.Requests[1].Content!.ReadAsStringAsync());
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, int, HttpResponseMessage> _responseFactory;

        internal RecordingHandler(Func<HttpRequestMessage, int, HttpResponseMessage> responseFactory)
            => _responseFactory = responseFactory;

        internal List<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            int requestIndex = Requests.Count;
            Requests.Add(request);
            return Task.FromResult(_responseFactory(request, requestIndex));
        }
    }
}
