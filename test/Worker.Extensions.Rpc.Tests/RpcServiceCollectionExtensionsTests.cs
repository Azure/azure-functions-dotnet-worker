// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Rpc.Tests
{
    public class RpcServiceCollectionExtensionsTests
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
            services.AddWorkerRpc();

            IServiceProvider sp = services.BuildServiceProvider();
            IOptions<FunctionsGrpcOptions> options = sp.GetService<IOptions<FunctionsGrpcOptions>>();
            Assert.NotNull(options);
            Assert.NotNull(options.Value);
            Assert.NotNull(options.Value.CallInvoker);
        }
    }
}
