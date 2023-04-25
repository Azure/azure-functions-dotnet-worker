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
    internal class ServiceHubContextProvider<THub, T> : ServiceHubContextProvider<THub> where THub : ServerlessHub<T>
        where T : class
    {
        public ServiceHubContextProvider(IConfiguration configuration, AzureComponentFactory azureComponentFactory, ILoggerFactory loggerFactory, Action<ServiceManagerBuilder>? configure = null) : base(configuration, azureComponentFactory, loggerFactory, configure)
        {
        }

        internal new ServiceHubContext<T>? ServiceHubContext { get; set; }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            using var serviceManager = CreateServiceManager();
            ServiceHubContext = await serviceManager.CreateHubContextAsync<T>(typeof(THub).Name, cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (ServiceHubContext != null)
            {
                await ServiceHubContext.DisposeAsync();
            }
            else
            {
                return;
            }
        }
    }
}
