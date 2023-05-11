// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Microsoft.Azure.Functions.Worker.SignalRService
{
    internal class ServiceHubContextInitializer<THub> : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly AzureComponentFactory _azureComponentFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Action<ServiceManagerBuilder>? _configure;
        private ServiceHubContext? _serviceHubContext;

        public ServiceHubContextInitializer(IConfiguration configuration, AzureComponentFactory azureComponentFactory, ILoggerFactory loggerFactory, HubContextProvider hubContextProvider, Action<ServiceManagerBuilder>? configure = null)
        {
            _configuration = configuration;
            _azureComponentFactory = azureComponentFactory;
            _loggerFactory = loggerFactory;
            _configure = configure;
            HubContextProvider = hubContextProvider;
        }

        protected HubContextProvider HubContextProvider { get; }

        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            using var serviceManager = CreateServiceManager();
            _serviceHubContext = await serviceManager.CreateHubContextAsync(typeof(THub).Name, cancellationToken);
            HubContextProvider.Add(typeof(THub), _serviceHubContext);
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_serviceHubContext != null)
            {
                await _serviceHubContext.DisposeAsync();
            }
        }

        protected ServiceManager CreateServiceManager()
        {
            var optionsSetup = new ServiceManagerOptionsSetup(_configuration, _azureComponentFactory, typeof(THub).GetCustomAttribute<ServerlessHub.SignalRConnectionAttribute>(true)?.ConnectionName ?? Constants.AzureSignalRConnectionStringName);
            var serviceManagerBuilder = new ServiceManagerBuilder()
                .WithOptions(optionsSetup.Configure)
                .WithLoggerFactory(_loggerFactory)
                .WithConfiguration(_configuration)
                .WithCallingAssembly()
                .AddUserAgent($" [{Constants.FunctionsWorkerProductInfoKey}={Constants.DotnetIsolatedWorker}");
            _configure?.Invoke(serviceManagerBuilder);
            var serviceManager = serviceManagerBuilder.BuildServiceManager();
            return serviceManager;
        }
    }
}
