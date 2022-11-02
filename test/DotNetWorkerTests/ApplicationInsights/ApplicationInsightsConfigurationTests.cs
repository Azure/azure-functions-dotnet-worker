using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.EventCounterCollector;
using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.ApplicationInsights.WindowsServer;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.ApplicationInsights.WorkerService.TelemetryInitializers;
using Microsoft.Azure.Functions.Worker.ApplicationInsights;
using Microsoft.Azure.Functions.Worker.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.ApplicationInsights;

public class ApplicationInsightsConfigurationTests
{
    [Fact]
    public void AddApplicationInsights_AddsDefaults()
    {
        var builder = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults(worker =>
            {
                worker
                    .AddApplicationInsights();
            });

        IEnumerable<ServiceDescriptor> initializers = null;
        IEnumerable<ServiceDescriptor> modules = null;

        builder.ConfigureServices(services =>
        {
            initializers = services.Where(s => s.ServiceType == typeof(ITelemetryInitializer));
            modules = services.Where(s => s.ServiceType == typeof(ITelemetryModule));
        });

        using (var host = builder.Build())
        {
            var provider = host.Services;

            Assert.Collection(initializers,
                t => Assert.Equal(typeof(FunctionsTelemetryInitializer), t.ImplementationType),
                t => Assert.Equal(typeof(AzureWebAppRoleEnvironmentTelemetryInitializer), t.ImplementationType),
                t => Assert.Equal(typeof(Microsoft.ApplicationInsights.WorkerService.TelemetryInitializers.DomainNameRoleInstanceTelemetryInitializer), t.ImplementationType),
                t => Assert.Equal(typeof(HttpDependenciesParsingTelemetryInitializer), t.ImplementationType),
                t => Assert.Equal(typeof(ComponentVersionTelemetryInitializer), t.ImplementationType));

            Assert.Collection(modules,
                t => Assert.Equal(typeof(FunctionsTelemetryModule), t.ImplementationType),
                t => Assert.Equal(typeof(DiagnosticsTelemetryModule), t.ImplementationType),
                t => Assert.Equal(typeof(AppServicesHeartbeatTelemetryModule), t.ImplementationType),
                t => Assert.Equal(typeof(AzureInstanceMetadataTelemetryModule), t.ImplementationType),
                t => Assert.Equal(typeof(PerformanceCollectorModule), t.ImplementationType),
                t => Assert.Equal(typeof(QuickPulseTelemetryModule), t.ImplementationType),
                t => Assert.Equal(typeof(DependencyTrackingTelemetryModule), t.ImplementationType),
                t => Assert.Equal(typeof(EventCounterCollectionModule), t.ImplementationType));

            var middleware = provider.GetRequiredService<FunctionActivitySourceMiddleware>();
            Assert.NotNull(middleware);
        }
    }

    [Fact]
    public void AddApplicationInsights_CallsConfigure()
    {
        bool called = false;
        var builder = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults(worker =>
            {
                worker.AddApplicationInsights(o =>
                {
                    Assert.NotNull(o);
                    called = true;
                });
            });

        Assert.False(called);

        using (var host = builder.Build())
        {
            var provider = host.Services;
            var options = provider.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>();
            Assert.NotNull(options.Value);

            var middleware = provider.GetRequiredService<FunctionActivitySourceMiddleware>();
            Assert.NotNull(middleware);

            Assert.True(called);
        }
    }

    [Fact]
    public void AddApplicationInsightsLogger_AddsDefaults()
    {
        var builder = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults(worker =>
            {
                worker.AddApplicationInsightsLogger();
            });

        bool called = false;

        builder.ConfigureServices(services =>
        {
            var loggerProviders = services.Where(s => s.ServiceType == typeof(ILoggerProvider));
            Assert.Collection(loggerProviders,
                t => Assert.Equal(typeof(WorkerLoggerProvider), t.ImplementationType),
                t => Assert.Equal(typeof(ApplicationInsightsLoggerProvider), t.ImplementationType));

            var initializers = services.Where(s => s.ServiceType == typeof(ITelemetryInitializer));
            Assert.Collection(initializers,
                t => Assert.Equal(typeof(FunctionsTelemetryInitializer), t.ImplementationType));

            var modules = services.Where(s => s.ServiceType == typeof(ITelemetryModule));
            Assert.Collection(modules,
                t => Assert.Equal(typeof(FunctionsTelemetryModule), t.ImplementationType));

            called = true;
        });

        using (var host = builder.Build())
        {
            var serviceProvider = host.Services;

            var appInsightsOptions = serviceProvider.GetRequiredService<IOptions<ApplicationInsightsLoggerOptions>>();
            Assert.False(appInsightsOptions.Value.IncludeScopes);

            var userWriter = serviceProvider.GetRequiredService<IUserLogWriter>();
            Assert.IsType<NullUserLogWriter>(userWriter);

            var systemWriter = serviceProvider.GetRequiredService<ISystemLogWriter>();
            Assert.IsNotType<NullLogWriter>(systemWriter);

            var middleware = serviceProvider.GetRequiredService<FunctionActivitySourceMiddleware>();
            Assert.NotNull(middleware);

            Assert.True(called);
        }
    }

    [Fact]
    public void AddApplicationInsightsLogger_CallsConfigure()
    {
        bool called = false;
        var builder = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults(worker =>
            {
                worker.AddApplicationInsightsLogger(o =>
                {
                    Assert.NotNull(o);
                    called = true;
                });
            });

        Assert.False(called);

        using (var host = builder.Build())
        {
            var provider = host.Services;
            var options = provider.GetRequiredService<IOptions<ApplicationInsightsLoggerOptions>>();
            Assert.NotNull(options.Value);

            var middleware = provider.GetRequiredService<FunctionActivitySourceMiddleware>();
            Assert.NotNull(middleware);

            Assert.True(called);
        }
    }

    [Fact]
    public void AddingServiceAndLogger_OnlyAddsServicesOnce()
    {
        var builder = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults(worker =>
            {
                worker
                    .AddApplicationInsights()
                    .AddApplicationInsightsLogger();
            });

        IEnumerable<ServiceDescriptor> initializers = null;
        IEnumerable<ServiceDescriptor> modules = null;

        builder.ConfigureServices(services =>
        {
            initializers = services.Where(s => s.ServiceType == typeof(ITelemetryInitializer));
            modules = services.Where(s => s.ServiceType == typeof(ITelemetryModule));
        });

        using (var host = builder.Build())
        {
            var provider = host.Services;

            // Ensure that our Initializer and Module are added alongside the defaults
            Assert.Collection(initializers,
                t => Assert.Equal(typeof(FunctionsTelemetryInitializer), t.ImplementationType),
                t => Assert.Equal(typeof(AzureWebAppRoleEnvironmentTelemetryInitializer), t.ImplementationType),
                t => Assert.Equal(typeof(Microsoft.ApplicationInsights.WorkerService.TelemetryInitializers.DomainNameRoleInstanceTelemetryInitializer), t.ImplementationType),
                t => Assert.Equal(typeof(HttpDependenciesParsingTelemetryInitializer), t.ImplementationType),
                t => Assert.Equal(typeof(ComponentVersionTelemetryInitializer), t.ImplementationType));

            Assert.Collection(modules,
                t => Assert.Equal(typeof(FunctionsTelemetryModule), t.ImplementationType),
                t => Assert.Equal(typeof(DiagnosticsTelemetryModule), t.ImplementationType),
                t => Assert.Equal(typeof(AppServicesHeartbeatTelemetryModule), t.ImplementationType),
                t => Assert.Equal(typeof(AzureInstanceMetadataTelemetryModule), t.ImplementationType),
                t => Assert.Equal(typeof(PerformanceCollectorModule), t.ImplementationType),
                t => Assert.Equal(typeof(QuickPulseTelemetryModule), t.ImplementationType),
                t => Assert.Equal(typeof(DependencyTrackingTelemetryModule), t.ImplementationType),
                t => Assert.Equal(typeof(EventCounterCollectionModule), t.ImplementationType));

            var middleware = provider.GetRequiredService<FunctionActivitySourceMiddleware>();
            Assert.NotNull(middleware);
        }
    }
}
