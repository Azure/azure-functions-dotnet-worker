using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.ApplicationInsights;

public class ApplicationInsightsConfigurationTests
{
    [Fact]
    public void AddApplicationInsights_AddsDefaults()
    {
        var builder = new HostBuilder().ConfigureServices(
            services => services.ConfigureFunctionsApplicationInsights());
        Verify(builder);
    }

    [Fact]
    public void AddingServiceAndLogger_OnlyAddsServicesOnce()
    {
        var builder = new HostBuilder().ConfigureServices(services =>
                services.ConfigureFunctionsApplicationInsights().ConfigureFunctionsApplicationInsights());
        Verify(builder);
    }

    private static void Verify(IHostBuilder builder)
    {
        builder.Build();
        IEnumerable<ServiceDescriptor> initializers = null;
        IEnumerable<ServiceDescriptor> modules = null;

        builder.ConfigureServices(services =>
        {
            initializers = services.Where(s => s.ServiceType == typeof(ITelemetryInitializer));
            modules = services.Where(s => s.ServiceType == typeof(ITelemetryModule));
        });

        Assert.Collection(initializers, t => Assert.Equal(typeof(FunctionsTelemetryInitializer), t.ImplementationType));
        Assert.Collection(modules, t => Assert.Equal(typeof(FunctionsTelemetryModule), t.ImplementationType));
    }
}
