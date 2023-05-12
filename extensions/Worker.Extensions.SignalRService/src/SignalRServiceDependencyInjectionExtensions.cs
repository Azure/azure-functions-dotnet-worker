// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Functions.Worker.SignalRService;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SignalRServiceDependencyInjectionExtensions
    {
        public static IServiceCollection AddServerlessHub<THub>(this IServiceCollection services) where THub : ServerlessHub
        {
            services.AddAzureClientsCore();
            services.TryAddSingleton<HubContextProvider>();
            services.TryAddSingleton<ServiceManagerOptionsSetup>();
            if (TryGetClientType(typeof(THub), out var clientType))
            {
                var genericDefinition = typeof(ServiceHubContextInitializer<,>);
                var genericType = genericDefinition.MakeGenericType(typeof(THub), clientType);
                return services.AddSingleton(sp => (IHostedService)ActivatorUtilities.CreateInstance(sp, genericType) as IHostedService);
            }
            else
            {
                return services.AddHostedService(sp => ActivatorUtilities.CreateInstance<ServiceHubContextInitializer<THub>>(sp));
            }
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
