// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker.Configuration
{
    public static class HostBuilderExtensions
    {
        public static HostBuilder ConfigureFunctionsWorker(this HostBuilder builder)
        {
            return builder.ConfigureFunctionsWorker(o => { });
        }

        public static HostBuilder ConfigureFunctionsWorker(this HostBuilder builder, Action<IFunctionsWorkerApplicationBuilder> configure)
        {
            return builder.ConfigureFunctionsWorker(configure);
        }

        public static IHostBuilder ConfigureFunctionsWorker(this IHostBuilder builder, Action<IFunctionsWorkerApplicationBuilder> configure, Action<WorkerOptions> configureOptions)
        {
            return builder.ConfigureFunctionsWorker((context, b) => configure(b), configureOptions);
        }

        public static IHostBuilder ConfigureFunctionsWorker(this IHostBuilder builder, Action<HostBuilderContext, IFunctionsWorkerApplicationBuilder> configure)
        {
            return builder.ConfigureFunctionsWorker(configure, o => { });
        }
        public static IHostBuilder ConfigureFunctionsWorker(this IHostBuilder builder, Action<HostBuilderContext, IFunctionsWorkerApplicationBuilder> configure, Action<WorkerOptions> configureOptions)
        {
            builder.ConfigureServices((context, services) =>
            {
                IFunctionsWorkerApplicationBuilder appBuilder = services.AddFunctionsWorker(configureOptions);

                configure(context, appBuilder);
            });

            return builder;
        }

        public static IFunctionsWorkerApplicationBuilder UseFunctionExecutionMiddleware(this IFunctionsWorkerApplicationBuilder builder)
        {
            // We want to keep this internal for now.
            builder.UseConverterMiddleware();

            builder.Services.AddSingleton<FunctionExecutionMiddleware>();

            builder.Use(next =>
            {
                return context =>
                {
                    var middleware = context.InstanceServices.GetRequiredService<FunctionExecutionMiddleware>();

                    return middleware.Invoke(context);
                };
            });

            return builder;
        }

        internal static IFunctionsWorkerApplicationBuilder UseConverterMiddleware(this IFunctionsWorkerApplicationBuilder builder)
        {
            builder.Services.AddSingleton<ConverterMiddleware>();

            builder.Use(next =>
            {
                return context =>
                {
                    var middleware = context.InstanceServices.GetRequiredService<ConverterMiddleware>();

                    return middleware.Invoke(context, next);
                };
            });

            return builder;
        }
    }
}
