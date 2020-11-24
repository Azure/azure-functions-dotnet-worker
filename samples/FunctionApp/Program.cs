using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Configuration;

namespace FunctionApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
#if DEBUG
            Debugger.Launch();
#endif
            var host = new HostBuilder()
                .ConfigureAppConfiguration(c =>
                {
                    c.AddCommandLine(args);
                })
                .ConfigureFunctionsWorker((c, b) =>
                {
                    b.UseFunctionExecutionMiddleware();
                })
                .ConfigureServices(s =>
                {
                    s.AddSingleton<IHttpResponderService, DefaultHttpResponderService>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
