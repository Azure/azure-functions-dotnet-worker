// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Testing.Tests.AspNetCore;

public class AspNetCoreFactoryTests
{
    [Fact]
    public async Task CreateClient_ExecutesNativeBindingRouteMiddlewareServiceAndActionResult()
    {
        await using var factory = CreateFactory().WithAspNetCore();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/orders/42",
            new { Name = "test-order" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("42", json.RootElement.GetProperty("id").GetString());
        Assert.Equal("test-order", json.RootElement.GetProperty("name").GetString());
        Assert.Equal("fixture-service", json.RootElement.GetProperty("marker").GetString());
        Assert.Equal("before", json.RootElement.GetProperty("middleware").GetString());
    }

    [Fact]
    public async Task CreateClient_ExecutesIResultAndWorkerOwnedRoute()
    {
        await using var factory = CreateFactory().WithAspNetCore();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/results/routed");

        response.EnsureSuccessStatusCode();
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("routed", json.RootElement.GetProperty("value").GetString());
    }

    [Fact]
    public async Task CreateClient_UnknownRouteAndMethodDoNotStartInvocation()
    {
        await using var factory = CreateFactory().WithAspNetCore();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage missing = await client.GetAsync("/api/missing");
        using HttpResponseMessage wrongMethod = await client.GetAsync("/api/orders/42");
        using HttpResponseMessage failedConstraint = await client.PostAsJsonAsync(
            "/api/orders/not-an-int",
            new { Name = "invalid" });

        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, wrongMethod.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, failedConstraint.StatusCode);
    }

    [Fact]
    public async Task WithAspNetCore_IsIdempotentClonedAndLateActivationFails()
    {
        await using var original = CreateFactory();
        await using var activated = original.WithAspNetCore().WithAspNetCore();

        Assert.Throws<NotSupportedException>(() => original.CreateClient());
        using HttpClient client = activated.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/api/results/clone");
        response.EnsureSuccessStatusCode();
        Assert.Throws<InvalidOperationException>(() => activated.WithAspNetCore());
    }

    [Fact]
    public void WithAspNetCore_MissingApplicationRegistrationFailsWithoutKestrelFallback()
    {
        using var factory = new FunctionsApplicationFactory<ModernFunctionApp.Program>()
            .WithContentRoot(GetModernFunctionOutput())
            .WithAspNetCore();

        Exception exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("ConfigureFunctionsWebApplication", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void CreateClient_UsesTestServerWithoutTcpAddress()
    {
        using var factory = CreateFactory().WithAspNetCore();
        using HttpClient client = factory.CreateClient();

        IServer server = factory.Services.GetRequiredService<IServer>();
        Assert.IsType<TestServer>(server);
    }

    [Fact]
    public async Task CreateClient_ParallelRequestsRemainCorrelated()
    {
        await using var factory = CreateFactory().WithAspNetCore();
        using HttpClient client = factory.CreateClient();

        Task<HttpResponseMessage>[] requests = Enumerable.Range(0, 12)
            .Select(index => client.GetAsync($"/api/results/value-{index}"))
            .ToArray();
        using var responses = new ResponseCollection(await Task.WhenAll(requests));

        for (int index = 0; index < responses.Items.Length; index++)
        {
            responses.Items[index].EnsureSuccessStatusCode();
            string json = await responses.Items[index].Content.ReadAsStringAsync();
            Assert.Contains($"value-{index}", json, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task CreateClient_RequestCancellationCancelsInvocation()
    {
        await using var factory = CreateFactory().WithAspNetCore();
        using HttpClient client = factory.CreateClient();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetAsync("/api/cancel", cancellation.Token));
    }

    [Fact]
    public async Task CreateClient_UserFunctionFailureIncludesFunctionDiagnostics()
    {
        await using var factory = CreateFactory().WithAspNetCore();
        using HttpClient client = factory.CreateClient();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetAsync("/api/fail"));

        Assert.Contains("AspNetFail", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ASP.NET fixture failure", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateClient_DefaultOptionsFollowRedirectAndHandleCookies()
    {
        await using var factory = CreateFactory().WithAspNetCore();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/cookies/set");

        response.EnsureSuccessStatusCode();
        Assert.Equal("cookie-value", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreateClient_DisabledRedirectReturnsRedirectAndHandlerDisposalDoesNotStopFactory()
    {
        await using var factory = CreateFactory().WithAspNetCore();
        using (HttpClient first = factory.CreateClient(
            new FunctionsTestClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            }))
        {
            using HttpResponseMessage redirect = await first.GetAsync("/api/cookies/set");
            Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
        }

        using HttpClient second = factory.CreateClient();
        using HttpResponseMessage response = await second.GetAsync("/api/results/alive");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public void CreateClient_DuplicateProviderFailsBeforeReturningClient()
    {
        using var factory = CreateFactory()
            .WithAspNetCore()
            .WithServices(services =>
                services.AddSingleton<IFunctionsTestHttpClientProvider, DuplicateProvider>());

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => factory.CreateClient());

        Assert.Contains("Multiple", exception.Message, StringComparison.Ordinal);
    }

    private static FunctionsApplicationFactory<AspNetCoreFunctionApp.Program> CreateFactory()
        => new FunctionsApplicationFactory<AspNetCoreFunctionApp.Program>()
            .WithContentRoot(GetAspNetCoreFunctionOutput());

    private static string GetAspNetCoreFunctionOutput()
        => Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "Resources",
            "Testing",
            "AspNetCoreFunctionApp",
            "bin",
            "Release",
            "net8.0"));

    private static string GetModernFunctionOutput()
        => Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "Resources",
            "Testing",
            "ModernFunctionApp",
            "bin",
            "Release",
            "net8.0"));

    private sealed class ResponseCollection : IDisposable
    {
        internal ResponseCollection(HttpResponseMessage[] items) => Items = items;

        internal HttpResponseMessage[] Items { get; }

        public void Dispose()
        {
            foreach (HttpResponseMessage response in Items)
            {
                response.Dispose();
            }
        }
    }

    private sealed class DuplicateProvider : IFunctionsTestHttpClientProvider
    {
        public HttpMessageHandler CreateHandler(FunctionsTestClientOptions options)
            => new HttpClientHandler();
    }
}
