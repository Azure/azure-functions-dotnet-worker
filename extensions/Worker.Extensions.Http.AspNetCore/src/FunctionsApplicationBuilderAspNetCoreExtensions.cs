// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
/// ASP.NET Core extensions for <see cref="FunctionsApplicationBuilder"/>.
/// </summary>
public static class FunctionsApplicationBuilderAspNetCoreExtensions
{
    /// <summary>
    /// Configures the worker to use the ASP.NET Core integration, enabling advanced HTTP features.
    /// </summary>
    /// <param name="builder">The <see cref="FunctionsApplicationBuilder"/> to configure.</param>
    /// <returns>The <see cref="FunctionsApplicationBuilder"/> for chaining.</returns>
    public static FunctionsApplicationBuilder ConfigureFunctionsWebApplication(this FunctionsApplicationBuilder builder)
    {
        builder.HostBuilder.ConfigureFunctionsWebApplication();
        return builder;
    }

    /// <summary>
    /// Configures the worker to use the ASP.NET Core integration, enabling advanced HTTP features.
    /// </summary>
    /// <param name="builder">The <see cref="FunctionsApplicationBuilder"/> to configure.</param>
    /// <param name="configureWorker">The worker configure callback.</param>
    /// <returns>The <see cref="FunctionsApplicationBuilder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configureWorker"/> is null.</exception>
    public static FunctionsApplicationBuilder ConfigureFunctionsWebApplication(this FunctionsApplicationBuilder builder, Action<IFunctionsWorkerApplicationBuilder> configureWorker)
    {
        ArgumentNullException.ThrowIfNull(configureWorker);
        builder.HostBuilder.ConfigureFunctionsWebApplication(configureWorker);
        return builder;
    }

    /// <summary>
    /// Configures the worker to use the ASP.NET Core integration, enabling advanced HTTP features.
    /// </summary>
    /// <param name="builder">The <see cref="FunctionsApplicationBuilder"/> to configure.</param>
    /// <param name="configureWorker">The worker configure callback receiving the host builder context.</param>
    /// <returns>The <see cref="FunctionsApplicationBuilder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configureWorker"/> is null.</exception>
    public static FunctionsApplicationBuilder ConfigureFunctionsWebApplication(this FunctionsApplicationBuilder builder, Action<HostBuilderContext, IFunctionsWorkerApplicationBuilder> configureWorker)
    {
        ArgumentNullException.ThrowIfNull(configureWorker);
        builder.HostBuilder.ConfigureFunctionsWebApplication(configureWorker);
        return builder;
    }
}
