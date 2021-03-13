using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;

namespace Hollan.Function
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string sqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
// #if DEBUG
//             Debugger.Launch();
// #endif
            var host = new HostBuilder()
                .ConfigureAppConfiguration(c =>
                {
                    c.AddCommandLine(args);
                })
                .ConfigureFunctionsWorker((c, b) =>
                {
                    b.UseFunctionExecutionMiddleware();
                })
                .ConfigureServices(s => {
                    s.AddDbContext<BloggingContext>(
                        options => options.UseSqlServer(sqlConnectionString)
                    );
                })
                .Build();

            await host.RunAsync();
        }
    }
}