// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.AspNetMiddleware;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Provides extension methods to work with a <see cref="IHostBuilder"/>.
    /// </summary>
    public static class FunctionsHostBuilderExtensions
    {
        /// <summary>
        /// Configures the worker to use the ASP.NET Core integration, enabling advanced HTTP features.
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public static IHostBuilder ConfigureFunctionsWebApplication(this IHostBuilder builder)
        {
            return builder.ConfigureFunctionsWebApplication(_ => { });
        }

        /// <summary>
        /// Configures the worker to use the ASP.NET Core integration, enabling advanced HTTP features.
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="configureWorker">The worker configure callback.</param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public static IHostBuilder ConfigureFunctionsWebApplication(this IHostBuilder builder, Action<IFunctionsWorkerApplicationBuilder> configureWorker)
        {
            builder.ConfigureFunctionsWorkerDefaults(workerAppBuilder =>
            {
                workerAppBuilder.UseAspNetCoreIntegration();
                configureWorker?.Invoke(workerAppBuilder);
            });

            builder.ConfigureAspNetCoreIntegration();

            return builder;
        }

        internal static IHostBuilder ConfigureAspNetCoreIntegration(this IHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<FunctionsEndpointDataSource>();
            });

            builder.ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls(HttpUriProvider.HttpUriString);
                webBuilder.Configure(b =>
                {
                    b.UseRouting();
                    b.UseMiddleware<WorkerRequestServicesMiddleware>();
                    // TODO: provide a way for customers to configure their middleware pipeline here                   
                    b.UseEndpoints(endpoints =>
                    {
                        var dataSource = endpoints.ServiceProvider.GetRequiredService<FunctionsEndpointDataSource>();
                        endpoints.DataSources.Add(dataSource);
                    });
                });
            });

            return builder;
        }
    }
}
