// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Functions.Worker.SignalRService
{
    internal class ServiceManagerOptionsSetup
    {
        private readonly IConfiguration _configuration;
        private readonly AzureComponentFactory _azureComponentFactory;
        private readonly IEnumerable<IConfigureOptions<ServiceManagerOptions>> _configureOptions;

        public ServiceManagerOptionsSetup(IConfiguration configuration, AzureComponentFactory azureComponentFactory, IEnumerable<IConfigureOptions<ServiceManagerOptions>> configureOptions)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _azureComponentFactory = azureComponentFactory;
            _configureOptions = configureOptions;
        }

        public Action<ServiceManagerOptions> Configure(string connectionStringName) => options =>
        {
            //make connection more stable
            options.ConnectionCount = 3;

            foreach (var configure in _configureOptions)
            {
                // ServiceManagerOptions contains a method (ServiceManagerOptions.UseJsonObjectSerialzier(ObjectSerializer)) which we want to make to also affect the final options.
                if (configure is IConfigureNamedOptions<ServiceManagerOptions> namedConfigure)
                {
                    namedConfigure.Configure(connectionStringName, options);
                }
                else if (connectionStringName == Options.DefaultName)
                {
                    configure.Configure(options);
                }
            }

            // The following setups read data from IConfiguration object. Consistent with in-process extension.
            if (_configuration.GetConnectionString(connectionStringName) != null || _configuration[connectionStringName] != null)
            {
                options.ConnectionString = _configuration.GetConnectionString(connectionStringName) ?? _configuration[connectionStringName];
            }
            var endpoints = _configuration.GetSection(Constants.AzureSignalREndpoints).GetEndpoints(_azureComponentFactory);

            // when the configuration is in the style: AzureSignalRConnectionString:serviceUri = https://xxx.service.signalr.net , we see the endpoint as unnamed.
            if (options.ConnectionString == null && _configuration.GetSection(connectionStringName).TryGetEndpointFromIdentity(_azureComponentFactory, out var endpoint, isNamed: false))
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
        };
    }
}
