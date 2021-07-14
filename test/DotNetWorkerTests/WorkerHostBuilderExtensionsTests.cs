// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class WorkerHostBuilderExtensionsTests
    {
        [Theory]
        [InlineData("--host", "127.0.0.1", "--port", "45040")]
        [InlineData("/home/usr/a.dll", "--host", "127.0.0.1", "--port", "45040")]
        [InlineData("/home/usr/a.dll", "/home/usr/a.dll", "--host", "127.0.0.1", "--port", "45040")]
        public void QuoteFirstArg(params string[] args)
        {
            var configBuilder = new ConfigurationBuilder();
            WorkerHostBuilderExtensions.RegisterCommandLine(configBuilder, args);

            var config = configBuilder.Build();

            Assert.Equal("127.0.0.1", config["host"]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void RegisterCommandLine_NoArgs(int count)
        {
            var args = Enumerable.Repeat<string>("test", count).ToArray();

            var configBuilder = new ConfigurationBuilder();
            WorkerHostBuilderExtensions.RegisterCommandLine(configBuilder, args);

            // Ensures we don't throw an IndexOutOfRangeException; no assert necessary.
            configBuilder.Build();
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

        [Fact]
        public void AzureFunctions_PrefixedVariables_AreRegistered()
        {
            string functionsEnvironment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
            Environment.SetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT", "Development");
            try
            {
                var host = new HostBuilder()
                    .ConfigureFunctionsWorkerDefaults()
                    .Build();

                var environment = host.Services.GetService<IHostEnvironment>();

                Assert.Equal("Development", environment.EnvironmentName);
            }
            finally
            {
                Environment.SetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT", functionsEnvironment);
            }
        }
    }
}
