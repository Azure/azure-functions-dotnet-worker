using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker.ApplicationInsights;
using Microsoft.Azure.Functions.Worker.ApplicationInsights.Initializers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.ApplicationInsights;

public class ApplicationInsightsConfigurationTests
{
    [Fact]
    public void AddApplicationInsights_SdkNotAdded_Throws()
    {
        var builder = new HostBuilder().ConfigureServices(
            services => services.ConfigureFunctionsApplicationInsights());

        try
        {
            builder.Build().Run();
        }
        catch (OptionsValidationException)
        {
        }
    }

    [Fact]
    public void AddApplicationInsights_AddsDefaults()
    {
        var builder = new HostBuilder().ConfigureServices(
            services => services
                .AddApplicationInsightsTelemetryWorkerService()
                .ConfigureFunctionsApplicationInsights());
        Verify(builder);
    }

    [Fact]
    public void AddingServiceAndLogger_OnlyAddsServicesOnce()
    {
        var builder = new HostBuilder().ConfigureServices(
            services => services
                .AddApplicationInsightsTelemetryWorkerService()
                .ConfigureFunctionsApplicationInsights()
                .ConfigureFunctionsApplicationInsights());
        Verify(builder);
    }

    private static void Verify(IHostBuilder builder)
    {
        IEnumerable<Type> initializers = null;
        IEnumerable<Type> modules = null;

        builder.ConfigureServices(services =>
        {
            initializers = services.Where(s => s.ServiceType == typeof(ITelemetryInitializer)).Select(x => x.ImplementationType);
            modules = services.Where(s => s.ServiceType == typeof(ITelemetryModule)).Select(x => x.ImplementationType);
        });

        builder.Build();
        Assert.Contains(typeof(FunctionsTelemetryInitializer), initializers);
        Assert.Equal(6, initializers.Count());
        Assert.Contains(typeof(FunctionsTelemetryModule), modules);
        Assert.Equal(8, modules.Count());
    }
}
