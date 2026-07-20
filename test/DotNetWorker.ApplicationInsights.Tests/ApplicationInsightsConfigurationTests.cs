// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.Azure.Functions.Worker.ApplicationInsights.Initializers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.ApplicationInsights.Tests;

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
    public void AddApplicationInsights_WithOptions_AddsDefaults()
    {
        var builder = new HostBuilder().ConfigureServices(
            services => services
                .AddApplicationInsightsTelemetryWorkerService()
                .ConfigureFunctionsApplicationInsights(o => o.MaxTelemetryBufferDelay = TimeSpan.FromSeconds(10)));
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

    [Fact]
    public void AddingServiceAndLogger_WithAndWithoutOptions_OnlyAddsServicesOnceAndAppliesOptions()
    {
        var builder = new HostBuilder().ConfigureServices(
            services => services
                .AddApplicationInsightsTelemetryWorkerService()
                .ConfigureFunctionsApplicationInsights()
                .ConfigureFunctionsApplicationInsights(o => o.MaxTelemetryBufferDelay = TimeSpan.FromSeconds(15)));

        using var host = builder.Build();

        // The second call is idempotent for registration but still applies the provided options.
        Assert.Single(host.Services.GetServices<ITelemetryModule>().OfType<FunctionsTelemetryModule>());
        var channel = Assert.IsType<ServerTelemetryChannel>(host.Services.GetRequiredService<TelemetryConfiguration>().TelemetryChannel);
        Assert.Equal(TimeSpan.FromSeconds(15), channel.MaxTelemetryBufferDelay);
    }

    [Fact]
    public void MaxTelemetryBufferDelay_DefaultsTo8Seconds()
    {
        var builder = new HostBuilder().ConfigureServices(
            services => services
                .AddApplicationInsightsTelemetryWorkerService()
                .ConfigureFunctionsApplicationInsights());

        using var host = builder.Build();
        var config = host.Services.GetRequiredService<TelemetryConfiguration>();

        var channel = Assert.IsType<ServerTelemetryChannel>(config.TelemetryChannel);
        Assert.Equal(TimeSpan.FromSeconds(8), channel.MaxTelemetryBufferDelay);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(20)]
    public void MaxTelemetryBufferDelay_CanBeOverridden(int seconds)
    {
        var builder = new HostBuilder().ConfigureServices(
            services => services
                .AddApplicationInsightsTelemetryWorkerService()
                .ConfigureFunctionsApplicationInsights(o => o.MaxTelemetryBufferDelay = TimeSpan.FromSeconds(seconds)));

        using var host = builder.Build();
        var config = host.Services.GetRequiredService<TelemetryConfiguration>();

        var channel = Assert.IsType<ServerTelemetryChannel>(config.TelemetryChannel);
        Assert.Equal(TimeSpan.FromSeconds(seconds), channel.MaxTelemetryBufferDelay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1)]
    [InlineData(4)]
    public void MaxTelemetryBufferDelay_ThrowsForValueBelowMinimum(int seconds)
    {
        var builder = new HostBuilder().ConfigureServices(
            services => services
                .AddApplicationInsightsTelemetryWorkerService()
                .ConfigureFunctionsApplicationInsights(o => o.MaxTelemetryBufferDelay = TimeSpan.FromSeconds(seconds)));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            using var host = builder.Build();
            host.Services.GetRequiredService<TelemetryConfiguration>();
        });
    }

    [Fact]
    public void ConfigureFunctionsApplicationInsights_NullOptions_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(
            () => services.ConfigureFunctionsApplicationInsights(configureOptions: null));
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
