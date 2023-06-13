// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Functions.Worker.SignalRService
{
    internal class ServiceManagerOptionsSetup
    {
        private readonly IConfiguration _configuration;
        private readonly Action<ServiceManagerOptions, string> _configure;

        public ServiceManagerOptionsSetup(IConfiguration configuration, AzureComponentFactory azureComponentFactory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _configure = (options, connectionStringKey) =>
            {
                if (_configuration.GetConnectionString(connectionStringKey) != null || _configuration[connectionStringKey] != null)
                {
                    options.ConnectionString = _configuration.GetConnectionString(connectionStringKey) ?? _configuration[connectionStringKey];
                }
                var endpoints = _configuration.GetSection(Constants.AzureSignalREndpoints).GetEndpoints(azureComponentFactory);

                // when the configuration is in the style: AzureSignalRConnectionString:serviceUri = https://xxx.service.signalr.net , we see the endpoint as unnamed.
                if (options.ConnectionString == null && _configuration.GetSection(connectionStringKey).TryGetEndpointFromIdentity(azureComponentFactory, out var endpoint, isNamed: false))
                {
                    endpoints = endpoints.Append(endpoint);
                }
                if (endpoints.Any())
                {
                    options.ServiceEndpoints = endpoints.ToArray();
                }
                var serviceTransportTypeStr = _configuration[Constants.ServiceTransportTypeName];
                if (Enum.TryParse<ServiceTransportType>(serviceTransportTypeStr, out var transport))
                {
                    options.ServiceTransportType = transport;
                }
                else if (!string.IsNullOrWhiteSpace(serviceTransportTypeStr))
                {
                    throw new InvalidOperationException($"Invalid service transport type: {serviceTransportTypeStr}.");
                }

                // Set the connection count of WebSockets connection if users use persistent mode.
                // This allows for fallback when one WebSockets connection is down.
                // The connections may be down due to maintenance or unexpected errors with the SignalR instance it connects to.
                // In such cases, the SDK can fall back to other WebSockets connections.
                options.ConnectionCount = 3;
            };
        }

        public Action<ServiceManagerOptions> Configure(string connectionStringKey) =>
            options => _configure(options, connectionStringKey);
    }
}
