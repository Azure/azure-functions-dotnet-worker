// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Functions.Worker.Testing;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Testing;

internal sealed class AspNetCoreTestHttpClientProvider : IFunctionsTestHttpClientProvider
{
    private readonly TestServer _server;

    public AspNetCoreTestHttpClientProvider(IServer server)
    {
        _server = server as TestServer
            ?? throw new InvalidOperationException(
                "ASP.NET Core testing activation did not replace the worker server with TestServer. "
                + "Verify package versions and call WithAspNetCore() before factory startup.");
    }

    public HttpMessageHandler CreateHandler(FunctionsTestClientOptions options)
        => new AspNetCoreTestClientHandler(_server.CreateHandler(), options);
}
