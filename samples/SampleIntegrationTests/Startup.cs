using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SampleIntegrationTests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGrpc(_ => { });
        services.AddSingleton<FunctionRpcTestServer>();
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
            var server = app.ApplicationServices.GetRequiredService<FunctionRpcTestServer>();

            endpoints.Map("/api/HttpTriggerSimple", context => server.HttpCall("HttpTriggerSimple",context));
            //endpoints.MapPost("/AzureFunctionsRpcMessages.FunctionRpc/EventStream", context => app.ApplicationServices.GetRequiredService<IMessageProcessor>().ProcessMessageAsync(context.Request.Body.Read()) )
            //endpoints.MapGet("/", async context =>
            //{
            //    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
            //});
        });
    }
}
