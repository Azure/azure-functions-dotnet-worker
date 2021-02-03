using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker.E2EApps.CosmosApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration(c =>
                {
                    c.AddCommandLine(args);
                })
                .ConfigureFunctionsWorker((c, b) =>
                {
                    b.UseFunctionExecutionMiddleware();

                    b.Services
                        .AddOptions<JsonSerializerOptions>()
                        .Configure(o => o.PropertyNameCaseInsensitive = true);
                })
                .Build();

            await host.RunAsync();
        }
    }
}
