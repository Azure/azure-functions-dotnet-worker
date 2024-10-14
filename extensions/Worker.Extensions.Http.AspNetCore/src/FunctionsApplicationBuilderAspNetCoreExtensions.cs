// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
}
