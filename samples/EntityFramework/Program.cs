// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.EntityFrameworkCore;
using System;

namespace Function
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
                .ConfigureFunctionsWorkerDefaults()
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