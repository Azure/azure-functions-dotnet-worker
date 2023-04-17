// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Provides extension methods to work with a <see cref="IHostBuilder"/>.
    /// </summary>
    public static class HostBuilderExtensions
    {

        // TODO: This should be modified to extend IFunctionsWorkerApplicationBuilder.
        /// <summary>
        /// Configures the worker to use the ASP.NET Core integration, enabling advanced HTTP features.
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public static IHostBuilder ConfigureAspNetCoreIntegration(this IHostBuilder builder)
        {
            builder.ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls(HttpUriProvider.GetHttpUri().ToString());
                webBuilder.Configure(b =>
                {
                    b.UseSetServiceProviderMiddleware();
                    // TODO: provide a way for customers to configure their middleware pipeline here
                    b.UseInvokeFunctionMiddleware();
                });
            });

            return builder;
        }
    }
}
