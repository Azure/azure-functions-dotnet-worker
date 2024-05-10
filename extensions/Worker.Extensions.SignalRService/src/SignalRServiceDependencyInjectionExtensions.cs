// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Functions.Worker.SignalRService;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SignalRServiceDependencyInjectionExtensions
    {
        public static IServiceCollection AddServerlessHub<THub>(this IServiceCollection services)
            where THub : ServerlessHub
            => services.AddServerlessHub(typeof(THub));

        public static IServiceCollection AddServerlessHub<THub>(this IServiceCollection services, Action<ServiceManagerBuilder> configure)
            where THub : ServerlessHub
            => services.AddServerlessHub(typeof(THub), configure);

        public static IServiceCollection AddServerlessHub(this IServiceCollection services, Type hubType)
           => services.AddServerlessHub(hubType, _ => { });

        public static IServiceCollection AddServerlessHub(this IServiceCollection services, Type hubType, Action<ServiceManagerBuilder> configure)
        {
            if (!typeof(ServerlessHub).IsAssignableFrom(hubType))
            {
                throw new ArgumentException($"{nameof(hubType)} is not derived from Microsoft.Azure.Functions.Worker.SignalRService.ServerlessHub.");
            }
            services.AddAzureClientsCore();
            services.TryAddSingleton<HubContextProvider>();
            services.TryAddSingleton<ServiceManagerOptionsSetup>();
            var genericType = TryGetClientType(hubType, out var clientType) ?
                typeof(ServiceHubContextInitializer<,>).MakeGenericType(hubType, clientType) :
                typeof(ServiceHubContextInitializer<>).MakeGenericType(hubType);
            // If we find a singleton generic type, then the hub is already registered, skip adding the hosted service.
            if (!services.Any(s => s.ServiceType == genericType && s.Lifetime == ServiceLifetime.Singleton))
            {
                services
                    .AddSingleton(genericType, sp => ActivatorUtilities.CreateInstance(sp, genericType, configure))
                    .AddSingleton(sp => (IHostedService)sp.GetRequiredService(genericType));
            }
            return services;
        }

        private static bool TryGetClientType(Type hubType, out Type clientType)
        {
            var serverlessHubOfTType = hubType.AllBaseTypes().FirstOrDefault(baseType => baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(ServerlessHub<>));
            if (serverlessHubOfTType != null)
            {
                clientType = serverlessHubOfTType.GetGenericArguments()[0];
                return true;
            }
            clientType = null;
            return false;
        }

        private static IEnumerable<Type> AllBaseTypes(this Type type)
        {
            Type current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }
    }
}
