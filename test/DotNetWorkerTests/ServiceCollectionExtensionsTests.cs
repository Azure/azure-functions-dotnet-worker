// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void ConfigureOptions_IsCalled()
        {
            var configured = false;
            var serviceColl = new ServiceCollection();
            serviceColl.AddFunctionsWorker(o =>
            {
                configured = true;
            });

            var services = serviceColl.BuildServiceProvider();

            // request the worker options, which forces their configuration to be called.
            var workerOptions = services.GetService<IOptions<WorkerOptions>>().Value;

            Assert.True(configured);
        }
    }
}
