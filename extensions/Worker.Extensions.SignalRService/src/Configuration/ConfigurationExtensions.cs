// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.Functions.Worker.SignalRService
{
    internal static class ConfigurationExtensions
    {
        public const string ServiceUriKey = "serviceUri";
        public const string ServerEndpointKey = "serverEndpoint";
        public const string ClientEndpointKey = "clientEndpoint";
        public const string TypeKey = "type";

        public static IEnumerable<ServiceEndpoint> GetEndpoints(this IConfiguration config, AzureComponentFactory azureComponentFactory)
        {
            foreach (var child in config.GetChildren())
            {
                if (child.TryGetNamedEndpointFromIdentity(azureComponentFactory, out var endpoint))
                {
                    yield return endpoint;
                    continue;
                }

                foreach (var item in child.GetNamedEndpointsFromConnectionString())
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<ServiceEndpoint> GetNamedEndpointsFromConnectionString(this IConfigurationSection section)
        {
            var endpointName = section.Key;
            if (section.Value != null)
            {
                yield return new ServiceEndpoint(section.Key, section.Value);
            }

            if (section["primary"] is string primary)
            {
                yield return new ServiceEndpoint(primary, EndpointType.Primary, endpointName);
            }

            if (section["secondary"] is string secondary)
            {
                yield return new ServiceEndpoint(secondary, EndpointType.Secondary, endpointName);
            }
        }

        public static bool TryGetNamedEndpointFromIdentity(this IConfigurationSection section, AzureComponentFactory azureComponentFactory, out ServiceEndpoint endpoint)
        {
            var text = section[ServiceUriKey];
            if (text != null)
            {
                var key = section.Key;
                var value = section.GetValue(TypeKey, EndpointType.Primary);
                var credential = azureComponentFactory.CreateTokenCredential(section);
                var serverEndpoint = section.GetValue<Uri>(ServerEndpointKey);
                var clientEndpoint = section.GetValue<Uri>(ClientEndpointKey);
                endpoint = new ServiceEndpoint(new Uri(text), credential, value, key, serverEndpoint, clientEndpoint);
                return true;
            }

            endpoint = null;
            return false;
        }

        public static bool TryGetEndpointFromIdentity(this IConfigurationSection section, AzureComponentFactory azureComponentFactory, out ServiceEndpoint endpoint, bool isNamed = true)
        {
            var text = section[ServiceUriKey];
            if (text != null)
            {
                var key = section.Key;
                var name = isNamed ? key : string.Empty;
                var value = section.GetValue(TypeKey, EndpointType.Primary);
                var credential = azureComponentFactory.CreateTokenCredential(section);
                var serverEndpoint = section.GetValue<Uri>(ServerEndpointKey);
                var clientEndpoint = section.GetValue<Uri>(ClientEndpointKey);
                endpoint = new ServiceEndpoint(new Uri(text), credential, value, name, serverEndpoint, clientEndpoint);
                return true;
            }

            endpoint = null;
            return false;
        }
    }
}
