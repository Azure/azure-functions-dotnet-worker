// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Provides extension methods to work with a <see cref="IFunctionsWorkerApplicationBuilder"/>.
    /// </summary>
    public static class WorkerBuilderExtensions
    {
        /// <summary>
        /// Adds the services needed to integrate with AspNetCore
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IFunctionsWorkerApplicationBuilder UseAspNetCoreIntegration(this IFunctionsWorkerApplicationBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.UseMiddleware<FunctionsHttpProxyingMiddleware>();

            builder.Services.AddSingleton<IHttpCoordinator, DefaultHttpCoordinator>();

            builder.Services.AddMvc();

            builder.Services.Configure<WorkerOptions>((workerOption) =>
            {
                workerOption.InputConverters.RegisterAt<HttpContextConverter>(0);
                workerOption.Capabilities.Add(Constants.HttpUriCapability, HttpUriProvider.HttpUriString);
            });

            FunctionsWorkerApplicationBuilderContext context = builder.GetContext();
            context.HostBuilder.ConfigureAspNetCoreIntegration();

            return builder;
        }
    }
}
