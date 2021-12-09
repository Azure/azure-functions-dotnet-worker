// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FunctionApp
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder()
                .Build();

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder()
        {
            return new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(s =>
                {
                    s.AddSingleton<IHttpResponderService, DefaultHttpResponderService>();
                });
        }
    }
}
