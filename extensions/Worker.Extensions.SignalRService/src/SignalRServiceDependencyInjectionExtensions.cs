// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.SignalRService;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SignalRServiceDependencyInjectionExtensions
    {
        public static IServiceCollection AddServerlessHub<THub>(this IServiceCollection services) where THub : ServerlessHub => services.AddServerlessHub<THub>(_ => { });

        public static IServiceCollection AddServerlessHub<THub>(this IServiceCollection services, Action<ServiceManagerBuilder> configure) where THub : ServerlessHub
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            services.AddAzureClientsCore();
            services.TryAddSingleton<HubContextCache>();
            services.TryAddSingleton(sp => ActivatorUtilities.CreateInstance<ServiceHubContextProvider<THub>>(sp, configure));
            return services.AddHostedService(sp => sp.GetRequiredService<ServiceHubContextProvider<THub>>());
        }

        public static IServiceCollection AddServerlessHub<THub, T>(this IServiceCollection services) where THub : ServerlessHub<T> where T : class => services.AddServerlessHub<THub, T>(_ => { });

        public static IServiceCollection AddServerlessHub<THub, T>(this IServiceCollection services, Action<ServiceManagerBuilder> configure) where THub : ServerlessHub<T> where T : class
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            services.AddAzureClientsCore();
            services.TryAddSingleton<HubContextCache>();
            services.TryAddSingleton(sp => ActivatorUtilities.CreateInstance<ServiceHubContextProvider<THub, T>>(sp, configure));
            return services.AddHostedService(sp => sp.GetRequiredService<ServiceHubContextProvider<THub, T>>());
        }
    }
}
