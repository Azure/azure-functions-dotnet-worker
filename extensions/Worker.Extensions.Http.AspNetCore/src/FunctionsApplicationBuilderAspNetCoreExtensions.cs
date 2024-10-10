// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Hosting;

public static class FunctionsApplicationBuilderAspNetCoreExtensions
{
    public static FunctionsApplicationBuilder ConfigureFunctionsWebApplication(this FunctionsApplicationBuilder builder)
    {
        builder.HostBuilder.ConfigureFunctionsWebApplication();
        return builder;
    }
}
