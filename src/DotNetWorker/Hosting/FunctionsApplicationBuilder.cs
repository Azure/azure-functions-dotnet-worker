using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Placeholder
/// </summary>
public class FunctionsApplicationBuilder : IHostApplicationBuilder
{
    private readonly HostApplicationBuilder _hostApplicationBuilder;

    internal FunctionsApplicationBuilder(Action<IHostBuilder> configureHostBuilder)
    {
        var configuration = new ConfigurationManager();

        _hostApplicationBuilder = new HostApplicationBuilder(new HostApplicationBuilderSettings
        {
            Configuration = configuration
        });

        var bootstrapHostBuilder = new BootstrapHostBuilder(_hostApplicationBuilder);
        configureHostBuilder(bootstrapHostBuilder);

        InitializeHosting(bootstrapHostBuilder);
    }

    /// <inheritdoc/>
    public IDictionary<object, object> Properties => ((IHostApplicationBuilder)_hostApplicationBuilder).Properties;

    IConfigurationManager IHostApplicationBuilder.Configuration => Configuration;

    /// <inheritdoc/>
    public ConfigurationManager Configuration => _hostApplicationBuilder.Configuration;

    /// <inheritdoc/>
    public IHostEnvironment Environment { get; private set; }

    /// <inheritdoc/>
    public ILoggingBuilder Logging => _hostApplicationBuilder.Logging;

    /// <inheritdoc/>
    public IMetricsBuilder Metrics => _hostApplicationBuilder.Metrics;

    /// <inheritdoc />
    public IServiceCollection Services => _hostApplicationBuilder.Services;

    /// <summary>
    /// Placeholder
    /// </summary>
    /// <returns>placeholder</returns>
    public IHost Build() => _hostApplicationBuilder.Build();

    /// <inheritdoc/>
    public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null) where TContainerBuilder : notnull
    {
        _hostApplicationBuilder.ConfigureContainer(factory, configure);
    }

    private void InitializeHosting(BootstrapHostBuilder bootstrapHostBuilder)
    {
        bootstrapHostBuilder.RunDefaultCallbacks();

        // Grab the HostBuilderContext from the property bag to use in the ConfigureWebHostBuilder. Then
        // grab the IWebHostEnvironment from the webHostContext. This also matches the instance in the IServiceCollection.
        //var hostContext = (HostBuilderContext)bootstrapHostBuilder.Properties[typeof(HostBuilderContext)];
        Environment = bootstrapHostBuilder.Context.HostingEnvironment;

        //Host = new ConfigureHostBuilder(bootstrapHostBuilder.Context, Configuration, Services);
        //WebHost = new ConfigureWebHostBuilder(webHostContext, Configuration, Services);
    }
}
