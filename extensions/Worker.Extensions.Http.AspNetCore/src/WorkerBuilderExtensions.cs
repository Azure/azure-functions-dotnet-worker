// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core.Http;
using Microsoft.Azure.Functions.Worker.Pipeline;
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

            // Add http coordinator
            builder.Services.AddSingleton<IHttpCoordinator, DefaultHttpCoordinator>();

            var port = Utilities.GetUnusedTcpPort().ToString();

            // temporarily use env vars until HostBuilderExtensions extends IFunctionsWorkerApplicationBuilder instead?
            Environment.SetEnvironmentVariable("FUNCTIONS_HTTP_PROXY_PORT", port);

            builder.Services.Configure<WorkerOptions>((workerOption) =>
            {
                workerOption.InputConverters.RegisterAt<HttpContextConverter>(0);
                workerOption.Capabilities.Add("EnableHttpProxying", port);
            });

            return builder;
        }
    }
}
