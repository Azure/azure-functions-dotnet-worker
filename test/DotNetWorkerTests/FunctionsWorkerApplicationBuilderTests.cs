// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class FunctionsWorkerApplicationBuilderTests
    {
        [Fact]
        public void ConfigureIsCalled()
        {
            bool configureBuilderCalled = false;
            bool configureWorkerOptionsCalled = false;
            IServiceProvider services = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(b => configureBuilderCalled = true, w => configureWorkerOptionsCalled = true)
                .Build()
                .Services;

            // request the worker options, which forces their configuration to be called.
            var workerOptions = services.GetService<IOptions<WorkerOptions>>().Value;

            Assert.True(configureBuilderCalled);
            Assert.True(configureWorkerOptionsCalled);
        }

        [Fact]
        public void GetContext_SetsHostBuilderAndContext_WhenConfigureIsCalled()
        {
            bool configureBuilderCalled = false;

            _ = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(b =>
                {
                    var context = b.GetContext();
                    Assert.NotNull(context.HostBuilderContext);
                    Assert.NotNull(context.HostBuilder);
                    configureBuilderCalled = true;
                })
                .Build();

            Assert.True(configureBuilderCalled);
        }

        [Fact]
        public void GetContext_NullHostBuilderAndContext_WhenAddIsCalled()
        {
            var services = new ServiceCollection();

            var builder = services.AddFunctionsWorkerDefaults();
            var context = builder.GetContext();
            Assert.Null(context.HostBuilderContext);
            Assert.Null(context.HostBuilder);
        }
    }
}
