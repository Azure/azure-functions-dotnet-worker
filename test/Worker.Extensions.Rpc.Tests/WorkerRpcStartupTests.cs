// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Rpc.Tests
{
    public class WorkerRpcStartupTests
    {
        [Fact]
        public void Configure_AddsFunctionsGrpcOptions()
        {
            int port = 21584; // random enough.
            ConfigurationBuilder configBuilder = new();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["HOST"] = "localhost",
                ["PORT"] = port.ToString(),
            });

            ServiceCollection services = new();
            services.AddSingleton((IConfiguration)configBuilder.Build());
            IFunctionsWorkerApplicationBuilder builder = Mock.Of<IFunctionsWorkerApplicationBuilder>(
                m => m.Services == services);

            WorkerRpcStartup startup = new();
            startup.Configure(builder);

            IServiceProvider sp = services.BuildServiceProvider();
            IOptions<FunctionsGrpcOptions> options = sp.GetService<IOptions<FunctionsGrpcOptions>>();
            Assert.NotNull(options);
            Assert.NotNull(options.Value);
            Assert.NotNull(options.Value.CallInvoker);
        }
    }
}
