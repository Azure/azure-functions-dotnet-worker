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
    }
}
