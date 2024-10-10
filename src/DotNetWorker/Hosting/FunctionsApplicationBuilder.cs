using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Hosting;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Placeholder
/// </summary>
public class FunctionsApplicationBuilder : IHostApplicationBuilder, IFunctionsWorkerApplicationBuilder
{
    private readonly HostApplicationBuilder _hostApplicationBuilder;
    private readonly BootstrapHostBuilder _bootstrapHostBuilder;
    private readonly IFunctionsWorkerApplicationBuilder _functionsWorkerApplicationBuilder;

    private bool _hasBuilt = false;

    internal const string SkipDefaultWorkerMiddlewareKey = "__SkipDefaultWorkerMiddleware";

    internal FunctionsApplicationBuilder(string[]? args)
    {
        var configuration = new ConfigurationManager();

        _hostApplicationBuilder = new HostApplicationBuilder(new HostApplicationBuilderSettings
        {
            Configuration = configuration
        });

        _bootstrapHostBuilder = new BootstrapHostBuilder(_hostApplicationBuilder);

        // We'll set the default middleware up just before we build.
        _bootstrapHostBuilder.Context.Properties[SkipDefaultWorkerMiddlewareKey] = true;

        _bootstrapHostBuilder
                   .ConfigureDefaults(args)
                   .ConfigureFunctionsWorkerDefaults();

        _functionsWorkerApplicationBuilder = InitializeHosting(_bootstrapHostBuilder);
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
    /// Only available to AspNetCore extension (and maybe not even there in the future)
    /// </summary>
    internal IHostBuilder HostBuilder => _bootstrapHostBuilder;

    /// <summary>
    /// Placeholder
    /// </summary>
    /// <returns>placeholder</returns>
    public IHost Build()
    {
        if (_hasBuilt)
        {
            throw new InvalidOperationException("The application has already been built.");
        }

        // we skipped this; make sure we add it at the end
        this.UseDefaultWorkerMiddleware();

        _hasBuilt = true;

        return _hostApplicationBuilder.Build();
    }

    /// <inheritdoc/>
    public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null) where TContainerBuilder : notnull
    {
        _hostApplicationBuilder.ConfigureContainer(factory, configure);
    }

    private IFunctionsWorkerApplicationBuilder InitializeHosting(BootstrapHostBuilder bootstrapHostBuilder)
    {
        bootstrapHostBuilder.ApplyServiceProviderFactory();
        bootstrapHostBuilder.RunAndClearRegisteredCallbacks();

        IFunctionsWorkerApplicationBuilder functionsWorkerApplicationBuilder = null!;

        foreach (var descriptor in Services)
        {
            if (descriptor.ServiceType == typeof(IFunctionsWorkerApplicationBuilder))
            {
                functionsWorkerApplicationBuilder = (IFunctionsWorkerApplicationBuilder)descriptor.ImplementationInstance!;
                break;
            }
        }

        // Grab the HostBuilderContext from the property bag to use in the ConfigureWebHostBuilder. Then
        // grab the IWebHostEnvironment from the webHostContext. This also matches the instance in the IServiceCollection.
        //var hostContext = (HostBuilderContext)bootstrapHostBuilder.Properties[typeof(HostBuilderContext)];
        Environment = bootstrapHostBuilder.Context.HostingEnvironment;

        //Host = new ConfigureHostBuilder(bootstrapHostBuilder.Context, Configuration, Services);
        //WebHost = new ConfigureWebHostBuilder(webHostContext, Configuration, Services);

        return functionsWorkerApplicationBuilder;
    }

    /// <inheritdoc/>
    public IFunctionsWorkerApplicationBuilder Use(Func<FunctionExecutionDelegate, FunctionExecutionDelegate> middleware)
    {
        if (_hasBuilt)
        {
            throw new InvalidOperationException("The application has already been built.");
        }

        return _functionsWorkerApplicationBuilder.Use(middleware);
    }
}
