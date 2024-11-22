// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using FuncApp = Microsoft.Azure.Functions.Worker.Builder.FunctionsApplication;

namespace Microsoft.Azure.Functions.Worker.Tests.Builder
{
    public class FunctionsApplicationBuilderTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("Development")]
        [InlineData("Staging")]
        [InlineData("Production")]
        [InlineData("Other")]
        public void CreateBuilder_AzureFunctionsEnvironment_IsSet(string value)
        {
            // Explicitly set to null to ensure this is not impacted by any existing value.
            Environment.SetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            if (!string.IsNullOrEmpty(value))
            {
                Environment.SetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT", value);
            }

            var builder = FuncApp.CreateBuilder([]);
            Assert.Equal(value, builder.Configuration["ENVIRONMENT"]);
            Assert.Equal(value ?? "Production", builder.Environment.EnvironmentName);

            IHost host = builder.Build();
            IConfiguration config = host.Services.GetService<IConfiguration>();
            IHostEnvironment env = host.Services.GetService<IHostEnvironment>();
            Assert.Equal(value, config["ENVIRONMENT"]);
            Assert.Equal(value ?? "Production", env.EnvironmentName);
        }
    }
}
