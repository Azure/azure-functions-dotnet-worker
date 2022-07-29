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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights.Tests;

public class ApplicationInsightsConfigurationTests
{
    [Fact]
    public void AddApplicationInsights_AddsDefaults()
    {
        var builder = new TestAppBuilder().AddApplicationInsights();

        // Ensure that our Initializer and Module are added alongside the defaults
        var initializers = builder.Services.Where(s => s.ServiceType == typeof(ITelemetryInitializer));
        Assert.Collection(initializers,
            t => Assert.Equal(typeof(FunctionsTelemetryInitializer), t.ImplementationType),
            t => Assert.Equal(typeof(AzureWebAppRoleEnvironmentTelemetryInitializer), t.ImplementationType),
            t => Assert.Equal(typeof(Microsoft.ApplicationInsights.WorkerService.TelemetryInitializers.DomainNameRoleInstanceTelemetryInitializer), t.ImplementationType),
            t => Assert.Equal(typeof(HttpDependenciesParsingTelemetryInitializer), t.ImplementationType),
            t => Assert.Equal(typeof(ComponentVersionTelemetryInitializer), t.ImplementationType));

        var modules = builder.Services.Where(s => s.ServiceType == typeof(ITelemetryModule));
        Assert.Collection(modules,
            t => Assert.Equal(typeof(FunctionsTelemetryModule), t.ImplementationType),
            t => Assert.Equal(typeof(DiagnosticsTelemetryModule), t.ImplementationType),
            t => Assert.Equal(typeof(AppServicesHeartbeatTelemetryModule), t.ImplementationType),
            t => Assert.Equal(typeof(AzureInstanceMetadataTelemetryModule), t.ImplementationType),
            t => Assert.Equal(typeof(PerformanceCollectorModule), t.ImplementationType),
            t => Assert.Equal(typeof(QuickPulseTelemetryModule), t.ImplementationType),
            t => Assert.Equal(typeof(DependencyTrackingTelemetryModule), t.ImplementationType),
            t => Assert.Equal(typeof(EventCounterCollectionModule), t.ImplementationType));
    }

    [Fact]
    public void AddApplicationInsights_CallsConfigure()
    {
        bool called = false;
        var builder = new TestAppBuilder().AddApplicationInsights(o =>
        {
            Assert.NotNull(o);
            called = true;
        });

        Assert.False(called);

        var provider = builder.Services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApplicationInsightsServiceOptions>>();
        Assert.NotNull(options.Value);

        Assert.True(called);
    }

    [Fact]
    public void AddApplicationInsightsLogger_AddsDefaults()
    {
        var builder = new TestAppBuilder().AddApplicationInsightsLogger();

        var loggerProviders = builder.Services.Where(s => s.ServiceType == typeof(ILoggerProvider));
        Assert.Collection(loggerProviders,
            t => Assert.Equal(typeof(ApplicationInsightsLoggerProvider), t.ImplementationType));

        var serviceProvider = builder.Services.BuildServiceProvider();
        var workerOptions = serviceProvider.GetRequiredService<IOptions<WorkerOptions>>();
        Assert.True(workerOptions.Value.DisableHostLogger);
    }

    [Fact]
    public void AddApplicationInsightsLogger_CallsConfigure()
    {
        bool called = false;
        var builder = new TestAppBuilder().AddApplicationInsightsLogger(o =>
        {
            Assert.NotNull(o);
            called = true;
        });

        Assert.False(called);

        var provider = builder.Services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApplicationInsightsLoggerOptions>>();
        Assert.NotNull(options.Value);

        Assert.True(called);

    }
}
