// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Adapted for Azure Functions from dotnet/aspnetcore DeferredHostBuilder:
// https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.Testing/src/DeferredHostBuilder.cs

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker.Testing.Hosting;

internal sealed class DeferredFunctionsHostBuilder : IHostBuilder
{
    private readonly ConfigurationManager _hostConfiguration = new();
    private readonly TaskCompletionSource _hostStart = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Action<IHostBuilder> _configure;
    private Func<string[], object>? _hostFactory;

    internal DeferredFunctionsHostBuilder()
    {
        _configure = builder =>
        {
            foreach ((object key, object value) in Properties)
            {
                builder.Properties[key] = value;
            }
        };
    }

    public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

    public IHost Build()
    {
        if (_hostFactory is null)
        {
            throw new InvalidOperationException("The entry-point host factory has not been assigned.");
        }

        var args = new List<string>();
        foreach ((string key, string? value) in _hostConfiguration.AsEnumerable())
        {
            args.Add($"--{key}={value}");
        }

        var host = (IHost)_hostFactory(args.ToArray());
        return new DeferredFunctionsHost(host, _hostStart);
    }

    public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
    {
        _configure += builder => builder.ConfigureAppConfiguration(configureDelegate);
        return this;
    }

    public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
    {
        _configure += builder => builder.ConfigureContainer(configureDelegate);
        return this;
    }

    public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
    {
        configureDelegate(_hostConfiguration);
        return this;
    }

    public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
    {
        _configure += builder => builder.ConfigureServices(configureDelegate);
        return this;
    }

    public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        where TContainerBuilder : notnull
    {
        _configure += builder => builder.UseServiceProviderFactory(factory);
        return this;
    }

    public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
        where TContainerBuilder : notnull
    {
        _configure += builder => builder.UseServiceProviderFactory(factory);
        return this;
    }

    internal void ConfigureHostBuilder(object hostBuilder)
    {
        if (hostBuilder is not IHostBuilder builder)
        {
            throw new InvalidOperationException(
                $"The application emitted a host builder of type '{hostBuilder.GetType().FullName}' that does not implement IHostBuilder.");
        }

        _configure(builder);
    }

    internal void EntryPointCompleted(Exception? exception)
    {
        if (exception is null)
        {
            _hostStart.TrySetResult();
        }
        else
        {
            _hostStart.TrySetException(exception);
        }
    }

    internal void SetHostFactory(Func<string[], object> hostFactory)
    {
        _hostFactory = hostFactory;
    }

    private sealed class DeferredFunctionsHost : IHost, IAsyncDisposable
    {
        private readonly IHost _host;
        private readonly TaskCompletionSource _hostStart;

        internal DeferredFunctionsHost(IHost host, TaskCompletionSource hostStart)
        {
            _host = host;
            _hostStart = hostStart;
        }

        public IServiceProvider Services => _host.Services;

        public void Dispose() => _host.Dispose();

        public async ValueTask DisposeAsync()
        {
            if (_host is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                _host.Dispose();
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            IHostApplicationLifetime lifetime = _host.Services.GetRequiredService<IHostApplicationLifetime>();
            using CancellationTokenRegistration started = lifetime.ApplicationStarted.UnsafeRegister(
                static state => ((TaskCompletionSource)state!).TrySetResult(),
                _hostStart);
            using CancellationTokenRegistration cancelled = cancellationToken.UnsafeRegister(
                static state => ((TaskCompletionSource)state!).TrySetCanceled(),
                _hostStart);

            await _hostStart.Task;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
            => _host.StopAsync(cancellationToken);
    }
}
