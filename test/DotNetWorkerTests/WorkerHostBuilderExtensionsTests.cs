// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Linq;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class WorkerHostBuilderExtensionsTests
    {
        [Theory]
        [InlineData("/home/usr/", "\"/home/usr/\"")]
        [InlineData(null, "--host")]
        public void QuoteFirstArg(string firstArg, string expected)
        {
            var cmdLineList = new List<string> { "--host", "127.0.0.1" };

            if (firstArg != null)
            {
                cmdLineList.Insert(0, firstArg);
            }

            var cmdLine = cmdLineList.ToArray();

            var configBuilder = new ConfigurationBuilder();
            WorkerHostBuilderExtensions.RegisterCommandLine(configBuilder, cmdLine);

            Assert.Equal(expected, cmdLine[0]);

            var config = configBuilder.Build();
            Assert.Equal("127.0.0.1", config["host"]);
        }

        [Fact]
        public void EnvironmentVariablesAreRegistered()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .Build();

            bool environmentVariablesProviderRegistered = ((ConfigurationRoot)host.Services.GetService<IConfiguration>())
                .Providers.Any(p => p is EnvironmentVariablesConfigurationProvider);

            Assert.True(environmentVariablesProviderRegistered, "Environment variables provider not registered.");
        }
    }
}
