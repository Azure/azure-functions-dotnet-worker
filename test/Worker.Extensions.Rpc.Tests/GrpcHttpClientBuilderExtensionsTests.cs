﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Grpc.Core;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Xunit;

#if NETFRAMEWORK
using Grpc.Net.Client.Web;
#endif

namespace Microsoft.Azure.Functions.Worker.Extensions.Rpc.Tests
{
    public class GrpcHttpClientBuilderExtensionsTests
    {
        [Fact]
        public void ConfigureForFunctionsHostGrpc_Configure_SetsUri()
            => ConfigureForFunctionsHostGrpc(s => s.AddGrpcClient<CallInvokerExtractor>(_ => { }));

        [Fact]
        public void ConfigureForFunctionsHostGrpc_SetsUri()
        {
            // https://github.com/grpc/grpc-dotnet/issues/2158
            // NOTE: when this test starts failing, it means the above issue was fixed.
            // We should update this test and documentation to reflect as such.
            InvalidOperationException exception = null;
            try
            {
                ConfigureForFunctionsHostGrpc(s => s.AddGrpcClient<CallInvokerExtractor>());
            }
            catch (InvalidOperationException ex)
            {
                exception = ex;
            }

            Assert.NotNull(exception);
        }

        private void ConfigureForFunctionsHostGrpc(Func<IServiceCollection, IHttpClientBuilder> configure)
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
            IHttpClientBuilder builder = configure(services);
            builder.ConfigureForFunctionsHostGrpc();

            // Capture the configured primary message handler.
            HttpMessageHandler handler = null;
            builder.Services.Configure(builder.Name, delegate (HttpClientFactoryOptions options)
            {
                options.HttpMessageHandlerBuilderActions.Add(b =>
                {
                    handler = b.PrimaryHandler;
                });
            });

            IServiceProvider sp = services.BuildServiceProvider();
            CallInvokerExtractor extractor = sp.GetService<CallInvokerExtractor>();

            Assert.NotNull(extractor);
            Assert.NotNull(extractor.CallInvoker);

            IOptionsMonitor<GrpcClientFactoryOptions> monitor = sp.GetService<IOptionsMonitor<GrpcClientFactoryOptions>>();
            GrpcClientFactoryOptions options = monitor.Get(builder.Name);

            Assert.Equal(new Uri($"http://localhost:{port}"), options.Address);

#if NETFRAMEWORK
            Assert.IsType<GrpcWebHandler>(handler);
#else
            Assert.Null(handler);
#endif
        }

        private class CallInvokerExtractor
        {
            public CallInvokerExtractor(CallInvoker callInvoker)
            {
                CallInvoker = callInvoker;
            }

            public CallInvoker CallInvoker { get; }
        }
    }
}
