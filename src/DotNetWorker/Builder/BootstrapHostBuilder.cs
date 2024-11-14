// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker.Builder
{
    /// <summary>
    /// Adapter to allow us to use our own <see cref="IHostBuilder"/> extensions with the
    /// <see cref="IHostApplicationBuilder"/>. Based on the BootstrapHostBuilder from the 
    /// AspNetCore source: https://github.com/dotnet/aspnetcore/blob/54c0cc8fa74e8196a2ce0711a20959143be7fb6f/src/DefaultBuilder/src/BootstrapHostBuilder.cs
    /// </summary>
    internal class BootstrapHostBuilder : IHostBuilder
    {
        private readonly HostApplicationBuilder _builder;

        private readonly List<Action<IConfigurationBuilder>> _configureHostActions = new();
        private readonly List<Action<HostBuilderContext, IConfigurationBuilder>> _configureAppActions = new();
        private readonly List<Action<HostBuilderContext, IServiceCollection>> _configureServicesActions = new();
        private IServiceProviderFactory<object>? _serviceProviderFactory;
        private bool _defaultsComplete = false;

        public BootstrapHostBuilder(HostApplicationBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));

            foreach (var descriptor in _builder.Services)
            {
                if (descriptor.ServiceType == typeof(HostBuilderContext))
                {
                    Context = (HostBuilderContext)descriptor.ImplementationInstance!;
                    break;
                }
            }

            if (Context is null)
            {
                throw new InvalidOperationException($"{nameof(HostBuilderContext)} must exist in the {nameof(IServiceCollection)}");
            }
        }

        public IDictionary<object, object> Properties => Context.Properties;

        public HostBuilderContext Context { get; }

        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            if (configureDelegate is null)
            {
                throw new ArgumentNullException(nameof(configureDelegate));
            }

            if (_defaultsComplete)
            {
                // After defaults are run, we want to run these immediately so that they are observable by the imperative code
                configureDelegate(Context, _builder.Configuration);
            }
            else
            {
                _configureAppActions.Add(configureDelegate);
            }

            return this;
        }

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            if (configureDelegate is null)
            {
                throw new ArgumentNullException(nameof(configureDelegate));
            }

            if (_defaultsComplete)
            {
                // After defaults are run, we want to run these immediately so that they are observable by the imperative code
                configureDelegate(_builder.Configuration);
            }
            else
            {
                _configureHostActions.Add(configureDelegate);
            }

            return this;
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            if (configureDelegate is null)
            {
                throw new ArgumentNullException(nameof(configureDelegate));
            }

            if (_defaultsComplete)
            {
                // After defaults are run, we want to run these immediately so that they are observable by the imperative code
                configureDelegate(Context, _builder.Services);
            }
            else
            {
                _configureServicesActions.Add(configureDelegate);
            }

            return this;
        }

        public IHost Build()
        {
            // Functions configuration will never call this.
            throw new InvalidOperationException();
        }

        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            // Functions configuration will never call this.
            throw new InvalidOperationException();
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull
        {
            _serviceProviderFactory = new ServiceProviderFactoryAdapter<TContainerBuilder>(factory);
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull
        {
            return UseServiceProviderFactory(factory(Context));
        }

        public void RunDefaultCallbacks()
        {
            foreach (var configureHostAction in _configureHostActions)
            {
                configureHostAction(_builder.Configuration);
            }

            // ConfigureAppConfiguration cannot modify the host configuration because doing so could
            // change the environment, content root and application name which is not allowed at this stage.
            foreach (var configureAppAction in _configureAppActions)
            {
                configureAppAction(Context, _builder.Configuration);
            }

            foreach (var configureServicesAction in _configureServicesActions)
            {
                configureServicesAction(Context, _builder.Services);
            }

            _defaultsComplete = true;
        }

        internal void ApplyServiceProviderFactory()
        {
            if (_serviceProviderFactory is null)
            {
                return;
            }

            _builder.ConfigureContainer(_serviceProviderFactory);
        }

        private sealed class ServiceProviderFactoryAdapter<TContainerBuilder> : IServiceProviderFactory<object> where TContainerBuilder : notnull
        {
            private readonly IServiceProviderFactory<TContainerBuilder> _serviceProviderFactory;

            public ServiceProviderFactoryAdapter(IServiceProviderFactory<TContainerBuilder> serviceProviderFactory)
            {
                _serviceProviderFactory = serviceProviderFactory;
            }

            public object CreateBuilder(IServiceCollection services) => _serviceProviderFactory.CreateBuilder(services);

            public IServiceProvider CreateServiceProvider(object containerBuilder) => _serviceProviderFactory.CreateServiceProvider((TContainerBuilder)containerBuilder);
        }
    }
}
