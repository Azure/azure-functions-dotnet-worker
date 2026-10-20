using System.Net;
using Microsoft.Azure.Functions.Worker.Testing;
using Xunit;

namespace AspNetCore.Tests;

public sealed class AspNetCoreFixture : IAsyncLifetime
{
    public FunctionsApplicationFactory<AspNetCoreFunctionApp.Program> Factory { get; } =
        new FunctionsApplicationFactory<AspNetCoreFunctionApp.Program>()
            .WithSetting("Sample:Mode", "Test")
            .WithContentRoot(Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..", "..",
                "test", "Resources", "Testing", "AspNetCoreFunctionApp",
                "bin", "Release", "net8.0")))
            .WithAspNetCore();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await Factory.DisposeAsync();
}

public class AspNetCoreSampleTests(AspNetCoreFixture fixture) : IClassFixture<AspNetCoreFixture>
{
    [Fact]
    public async Task TestServer_UsesWorkerOwnedRoutesWithoutTcp()
    {
        using HttpClient client = fixture.Factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/api/results/sample");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("sample", await response.Content.ReadAsStringAsync());
    }
}
