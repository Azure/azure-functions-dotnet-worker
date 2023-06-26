// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Microsoft.Azure.Functions.Worker.SignalRService
{
    internal class ServiceHubContextInitializer<THub> : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ServiceManagerOptionsSetup _optionsSetup;
        private readonly Action<ServiceManagerBuilder>? _configure;
        private ServiceHubContext? _serviceHubContext;

        public ServiceHubContextInitializer(IConfiguration configuration, ILoggerFactory loggerFactory, HubContextProvider hubContextProvider, ServiceManagerOptionsSetup optionsSetup, Action<ServiceManagerBuilder>? configure)
        {
            _configuration = configuration;
            _loggerFactory = loggerFactory;
            HubContextProvider = hubContextProvider;
            _optionsSetup = optionsSetup;
            _configure = configure;
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
            var serviceManagerBuilder = new ServiceManagerBuilder()
                .WithOptions(_optionsSetup.Configure(typeof(THub).GetCustomAttribute<ServerlessHub.SignalRConnectionAttribute>(true)?.ConnectionName ?? ServerlessHub.DefaultConnectionStringName))
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
