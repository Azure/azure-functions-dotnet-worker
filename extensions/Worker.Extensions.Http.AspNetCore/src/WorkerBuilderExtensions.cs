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
    public static class WorkerBuilderExtensions
    {
        public static IFunctionsWorkerApplicationBuilder UseAspNetCoreIntegration(this IFunctionsWorkerApplicationBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.UseMiddleware<FunctionsHttpProxyingMiddleware>();

            // Add http coordinator
            builder.Services.AddSingleton<IHttpCoordinator, DefaultHttpCoordinator>();

            builder.Services.Configure<WorkerOptions>((workerOption) =>
            {
                workerOption.InputConverters.RegisterAt<HttpContextConverter>(0);
                workerOption.Capabilities.Add("EnableHttpProxying", bool.TrueString);
            });

            return builder;
        }
    }
}
