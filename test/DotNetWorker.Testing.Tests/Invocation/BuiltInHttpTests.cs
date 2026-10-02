// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Testing.Tests.Invocation;

public class BuiltInHttpTests
{
    [Fact]
    public async Task BuiltInHttp_FunctionTargetPreservesRequestAndMapsResponse()
    {
        await using var factory = CreateFactory();
        using HttpClient client = factory.CreateHttpClient(
            "ModernEcho",
            new FunctionsHttpClientOptions { BaseAddress = new Uri("https://functions.test") });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/orders/42?mode=full")
        {
            Content = new StringContent("payload", Encoding.UTF8, "text/plain")
        };
        request.Headers.Add("x-test-header", "header-value");

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("POST", Assert.Single(response.Headers.GetValues("x-echo-method")));
        Assert.Equal(
            "/orders/42?mode=full",
            Assert.Single(response.Headers.GetValues("x-echo-path-query")));
        Assert.Equal("header-value", Assert.Single(response.Headers.GetValues("x-echo-header")));
        Assert.Equal("payload", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task BuiltInHttp_UnknownFunctionAndMissingCompanionFailWithGuidance()
    {
        await using var factory = CreateFactory();

        Assert.Throws<InvalidOperationException>(() => factory.CreateHttpClient("missing"));
        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => factory.CreateClient());
        Assert.Contains("CreateHttpClient(functionName)", exception.Message, StringComparison.Ordinal);
        Assert.Contains("WithAspNetCore()", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuiltInHttp_NonHttpFunctionFailsBeforeClientCreation()
    {
        await using var factory = CreateFactory();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => factory.CreateHttpClient("NonHttpFunction"));

        Assert.Contains("exactly one httpTrigger", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuiltInHttp_DoesNotEnforceHostAuthorization()
    {
        await using var factory = CreateFactory();
        using HttpClient client = factory.CreateHttpClient("AuthorizedEcho");

        using HttpResponseMessage response = await client.PostAsync(
            "/any-url-without-a-function-key",
            new StringContent("authorized-payload"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("authorized-payload", await response.Content.ReadAsStringAsync());
        Assert.False(factory.Capabilities.SupportsAuthorization);
    }

    [Fact]
    public void Capabilities_DeclareEveryHostOwnedBehaviorUnavailable()
    {
        using var factory = CreateFactory();

        Assert.False(factory.Capabilities.SupportsBuiltInUrlRouting);
        Assert.False(factory.Capabilities.SupportsAuthorization);
        Assert.False(factory.Capabilities.SupportsTriggerListeners);
        Assert.False(factory.Capabilities.SupportsMessageSettlement);
        Assert.False(factory.Capabilities.SupportsRetryScheduling);
        Assert.StartsWith("https://", factory.Capabilities.FullHostTestingGuidance, StringComparison.Ordinal);
    }

    [Fact]
    public void BuiltInHttp_InvalidBaseAddressFailsBeforeFactoryStartup()
    {
        using var factory = CreateFactory();
        var options = new FunctionsHttpClientOptions { BaseAddress = new Uri("file:///tmp/function") };

        Assert.Throws<ArgumentException>(() => factory.CreateHttpClient("ModernEcho", options));
        _ = factory.WithSetting("still", "configurable");
    }

    private static FunctionsApplicationFactory<ModernFunctionApp.Program> CreateFactory()
        => new FunctionsApplicationFactory<ModernFunctionApp.Program>()
            .WithContentRoot(GetFunctionOutput());

    private static string GetFunctionOutput()
        => Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
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
}
