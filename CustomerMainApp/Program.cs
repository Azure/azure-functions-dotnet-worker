using System.Diagnostics;
using System.Threading.Tasks;
using FunctionsDotNetWorker.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CustomerMainApp
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
                .ConfigureDotNetWorker((c, b) =>
                {
                })
                .Build();

            await host.RunAsync();
        }
    }
}
