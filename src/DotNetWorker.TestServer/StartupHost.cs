using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker.TestServer;

public class StartupHost
{
    private const int MaxMessageLengthBytes = int.MaxValue;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGrpc(options =>
        {
            options.MaxReceiveMessageSize = MaxMessageLengthBytes;
            options.MaxSendMessageSize = MaxMessageLengthBytes;
        });
        services.WithRpcTestServer();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<FunctionRpcTestServer>();
        });

        
    }
}
