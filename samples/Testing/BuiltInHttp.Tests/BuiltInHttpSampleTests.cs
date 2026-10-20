using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Worker.Testing;
using Xunit;

namespace BuiltInHttp.Tests;

public sealed class BuiltInHttpFixture : IAsyncLifetime
{
    public FunctionsApplicationFactory<ModernFunctionApp.Program> Factory { get; } =
        new FunctionsApplicationFactory<ModernFunctionApp.Program>()
            .WithSetting("Sample:Mode", "Test")
            .WithContentRoot(Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..", "..",
                "test", "Resources", "Testing", "ModernFunctionApp",
                "bin", "Release", "net8.0")));

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await Factory.DisposeAsync();
}

public class BuiltInHttpSampleTests(BuiltInHttpFixture fixture) : IClassFixture<BuiltInHttpFixture>
{
    [Fact]
    public async Task FunctionTargetedHttp_UsesWorkerPipeline()
    {
        using HttpClient client = fixture.Factory.CreateHttpClient("ModernEcho");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/orders/42?mode=sample")
        {
            Content = new StringContent("sample-payload", Encoding.UTF8, "text/plain")
        };

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("sample-payload", await response.Content.ReadAsStringAsync());
        Assert.False(fixture.Factory.Capabilities.SupportsBuiltInUrlRouting);
        Assert.False(fixture.Factory.Capabilities.SupportsAuthorization);
    }
}
