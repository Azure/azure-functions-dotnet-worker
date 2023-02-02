using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.TestServer;

/// <summary>
/// Allow to register test server
/// </summary>
public static class TestServerServiceCollectionExtensions
{
    private const int MaxMessageLengthBytes = int.MaxValue;

    public static IServiceCollection WithRpcTestServer(this IServiceCollection services)
    {
        services.AddGrpc(options =>
        {
            options.MaxReceiveMessageSize = MaxMessageLengthBytes;
            options.MaxSendMessageSize = MaxMessageLengthBytes;
        });
        services.AddSingleton<FunctionRpcTestServer>();
        services.AddSingleton<ITestServer>(provider => provider.GetRequiredService<FunctionRpcTestServer>());
        return services;
    }

    public static IEndpointRouteBuilder MapTestServer(this IEndpointRouteBuilder builder)
    {
        builder.MapGrpcService<FunctionRpcTestServer>();
        return builder;
    }
}
