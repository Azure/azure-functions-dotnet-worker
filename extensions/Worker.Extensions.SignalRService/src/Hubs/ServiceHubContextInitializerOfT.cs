// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Microsoft.Azure.Functions.Worker.SignalRService
{
    internal class ServiceHubContextInitializer<THub, T> : ServiceHubContextInitializer<THub> where THub : ServerlessHub<T>
        where T : class
    {
        private ServiceHubContext<T>? _serviceHubContext;

        public ServiceHubContextInitializer(IConfiguration configuration, AzureComponentFactory azureComponentFactory, ILoggerFactory loggerFactory, HubContextProvider hubContextProvider, Action<ServiceManagerBuilder>? configure = null) : base(configuration, azureComponentFactory, loggerFactory, hubContextProvider, configure)
        {
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            using var serviceManager = CreateServiceManager();
            _serviceHubContext = await serviceManager.CreateHubContextAsync<T>(typeof(THub).Name, cancellationToken);
            HubContextProvider.Add(typeof(THub), _serviceHubContext);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_serviceHubContext != null)
            {
                await _serviceHubContext.DisposeAsync();
            }
        }
    }
}
