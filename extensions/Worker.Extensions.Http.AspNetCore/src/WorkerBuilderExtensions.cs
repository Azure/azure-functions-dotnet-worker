// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Provides extension methods to work with a <see cref="IFunctionsWorkerApplicationBuilder"/>.
    /// </summary>
    internal static class WorkerBuilderExtensions
    {
        /// <summary>
        /// Adds the services needed to integrate with AspNetCore
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static IFunctionsWorkerApplicationBuilder UseAspNetCoreIntegration(this IFunctionsWorkerApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder, nameof(builder));

            // Check if already configured by looking for our middleware
            if (builder.Services.Any(d => d.ImplementationType == typeof(FunctionsHttpProxyingMiddleware)))
            {
                return builder;
            }

            builder.UseMiddleware<FunctionsHttpProxyingMiddleware>();

            builder.Services.AddSingleton<IHttpCoordinator, DefaultHttpCoordinator>();

            builder.Services.AddMvc();

            builder.Services.Configure<WorkerOptions>((workerOption) =>
            {
                workerOption.InputConverters.RegisterAt<HttpContextConverter>(0);
                workerOption.Capabilities[Constants.HttpUriCapability] = HttpUriProvider.HttpUriString;
            });

            return builder;
        }
    }
}
