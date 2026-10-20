// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.AspNetMiddleware;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker.Testing;

/// <summary>Provides explicit ASP.NET Core TestServer activation for a functions application factory.</summary>
public static class AspNetCoreFunctionsApplicationFactoryExtensions
{
    private const string ActivationId = "Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Testing";

    /// <summary>
    /// Returns an independent unstarted factory configured to use the worker ASP.NET Core integration through TestServer.
    /// </summary>
    public static FunctionsApplicationFactory<TEntryPoint> WithAspNetCore<TEntryPoint>(
        this FunctionsApplicationFactory<TEntryPoint> factory)
        where TEntryPoint : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        Assembly companionAssembly = typeof(AspNetCoreFunctionsApplicationFactoryExtensions).Assembly;
        AssemblyCompatibilityValidator.ValidateReferencedAssembly(
            companionAssembly,
            typeof(FunctionsApplicationFactory<TEntryPoint>).Assembly);
        AssemblyCompatibilityValidator.ValidateReferencedAssembly(
            companionAssembly,
            typeof(FunctionsEndpointDataSource).Assembly);
        AssemblyCompatibilityValidator.ValidateReferencedAssembly(companionAssembly, typeof(TestServer).Assembly);

        return factory.WithHttpCompanion(ActivationId, builder =>
        {
            builder.ConfigureWebHost(webBuilder => webBuilder.UseTestServer());
            builder.ConfigureServices((_, services) =>
            {
                if (!services.Any(descriptor => descriptor.ServiceType == typeof(IHttpCoordinator)))
                {
                    throw new InvalidOperationException(
                        "WithAspNetCore() requires the function application to call ConfigureFunctionsWebApplication().");
                }

                services.Replace(ServiceDescriptor.Singleton<FunctionsEndpointDataSource>(provider =>
                    new FunctionsEndpointDataSource(
                        provider.GetRequiredService<IFunctionMetadataManager>(),
                        provider.GetRequiredService<IFunctionsTestInvocationDispatcher>().ApplicationDirectory)));
                services.AddSingleton<IFunctionsHttpRequestDispatcher, FunctionsTestingDispatchMiddleware>();
                services.AddSingleton<IFunctionsTestHttpClientProvider, AspNetCoreTestHttpClientProvider>();
            });
        });
    }
}
