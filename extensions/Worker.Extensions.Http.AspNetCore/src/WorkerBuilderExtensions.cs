// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core.Http;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Coordinator;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.FunctionsMiddleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNet
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

            // Add http coordinator; one-per-invocation
            builder.Services.AddScoped<IHttpCoordinator, DefaultHttpCoordinator>();

            var port = Utilities.GetUnusedTcpPort().ToString();

            builder.Services.Configure<WorkerOptions>((workerOption) =>
            {
                workerOption.InputConverters.RegisterAt<HttpContextConverter>(0);
                workerOption.Capabilities.Add(Constants.HttpUriCapability, HttpUriProvider.GetHttpUri().ToString()); // testing host side, remove this const later
            });

            return builder;
        }
    }
}
