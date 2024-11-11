// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Azure.Functions.Worker.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void ConfigureOptions_IsCalled()
    {
        var configured = false;
        var serviceColl = new ServiceCollection();
        serviceColl.AddFunctionsWorkerDefaults(o =>
        {
            configured = true;
        });

        var services = serviceColl.BuildServiceProvider();

        // request the worker options, which forces their configuration to be called.
        var workerOptions = services.GetService<IOptions<WorkerOptions>>().Value;

        Assert.True(configured);
    }

    [Fact]
    public void DefaultInputConverters_RegisteredOnce()
    {
        var serviceColl = new ServiceCollection();
        serviceColl.AddFunctionsWorkerDefaults();
        serviceColl.AddFunctionsWorkerDefaults();

        var services = serviceColl.BuildServiceProvider();

        // request the worker options, which forces their configuration to be called.
        var workerOptions = services.GetService<IOptions<WorkerOptions>>().Value;

        // Ensure that even though we've called the registration twice, only one
        // set of default input converters is registered.
        var count = workerOptions.InputConverters.Count();
        Assert.Equal(9, count);
    }

    [Fact]
    public void LoggerProvider_RegisteredOnce()
    {
        var serviceColl = new ServiceCollection();
        serviceColl.AddFunctionsWorkerDefaults();
        serviceColl.AddFunctionsWorkerDefaults();

        var services = serviceColl.BuildServiceProvider();

        var loggerProviders = services.GetServices<ILoggerProvider>();

        // Ensure that even though we've called the registration twice, only one
        // WorkerLoggerProvider is registered.
        Assert.Single(loggerProviders.Where(p => p is WorkerLoggerProvider));
    }

    [Fact]
    public void SameBuilder_Returned()
    {
        var serviceColl = new ServiceCollection();
        var builder1 = serviceColl.AddFunctionsWorkerCore();
        var builder2 = serviceColl.AddFunctionsWorkerCore();

        Assert.Same(builder1, builder2);
    }

    [Fact]
    public void AddFunctionsWorkerCore_RegistersServicesIdempotently()
    {
        ServiceCollectionExtensionsTestUtility.AssertServiceRegistrationIdempotency(services =>
        {
            services.AddFunctionsWorkerCore();
            services.AddFunctionsWorkerCore();
        });
    }
}
