﻿// Copyright (c) .NET Foundation. All rights reserved.
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
        private readonly Action<ServiceManagerOptions> _configureServiceManagerOptions;

        public ServiceManagerOptionsSetup(IConfiguration configuration, AzureComponentFactory azureComponentFactory, string connectionStringKey)
        {
            if (string.IsNullOrWhiteSpace(connectionStringKey))
            {
                throw new ArgumentException($"'{nameof(connectionStringKey)}' cannot be null or whitespace", nameof(connectionStringKey));
            }

            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _configureServiceManagerOptions = options =>
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
                //make connection more stable
                options.ConnectionCount = 3;
            };
        }

        public Action<ServiceManagerOptions> Configure => _configureServiceManagerOptions;
    }
}