// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Hosting helpers to work with Azure Functions Workers
    /// </summary>
    public static class FunctionsWorkerHost
    {
        /// <summary>
        /// Creates a default Azure Functions Worker configured host, runs and 
        /// blocks the calling thread until the host shuts down.
        /// </summary>
        /// <param name="configureServices">An optional delegate to configure host services.</param>
        public static void RunDefault(Action<IServiceCollection>? configureServices = null)
        {
            RunDefaultAsync(configureServices).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates a default Azure Functions Worker configured host, runs and
        /// returns a <see cref="Task"/> that will complete when the host shuts down.
        /// </summary>
        /// <param name="configureService">An option delegate to configure host services.</param>
        /// <returns>A <see cref="Task"/> that will complete when the host shuts down.</returns>
        public async static Task RunDefaultAsync(Action<IServiceCollection>? configureService = null)
        {
            var builder = Host.CreateDefaultBuilder()
                 .ConfigureFunctionsWorkerDefaults();

            if (configureService is not null)
            {
                builder.ConfigureServices(configureService);
            }

            await builder.Build().RunAsync();
        }
    }
}
