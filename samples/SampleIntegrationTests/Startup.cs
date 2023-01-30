using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Functions.Worker.TestServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SampleIntegrationTests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
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
            endpoints.MapTestServer();
        });
    }
}
