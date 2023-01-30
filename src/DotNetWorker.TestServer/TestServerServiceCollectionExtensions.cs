using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.TestServer;

/// <summary>
/// Allow to register test server
/// </summary>
public static class TestServerServiceCollectionExtensions
{
    public static IServiceCollection WithRpcTestServer(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddGrpc(_ => { });
        serviceCollection.AddSingleton<FunctionRpcTestServer>();
        serviceCollection.AddSingleton<ITestServer>(provider => provider.GetRequiredService<FunctionRpcTestServer>());
        return serviceCollection;
    }

    public static IEndpointRouteBuilder MapTestServer(this IEndpointRouteBuilder builder)
    {
        builder.MapGrpcService<FunctionRpcTestServer>();
        return builder;
    }
}
