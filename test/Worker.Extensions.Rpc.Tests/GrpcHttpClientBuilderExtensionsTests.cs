// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#if NET6_0_OR_GREATER

using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Rpc.Tests
{
    public class GrpcHttpClientBuilderExtensionsTests
    {
        [Fact]
        public void ConfigureForFunctionsHostGrpc_Configure_SetsUri()
            => ConfigureForFunctionsHostGrpc(s => s.AddGrpcClient<CallInvokerExtractor>(_ => { }));

        [Fact]
        public void ConfigureForFunctionsHostGrpc_SetsUri()
            => ConfigureForFunctionsHostGrpc(s => s.AddGrpcClient<CallInvokerExtractor>());

        [Fact]
        public void ConfigureForFunctionsHostGrpc_SetsMessageSize()
            => ConfigureForFunctionsHostGrpc(s => s.AddGrpcClient<CallInvokerExtractor>(), Random.Shared.Next(4098, 10000));

        private void ConfigureForFunctionsHostGrpc(Func<IServiceCollection, IHttpClientBuilder> configure, int? maxMessageLength = null)
        {
            int port = 21584; // random enough.
            ConfigurationBuilder configBuilder = new();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["HOST"] = "localhost",
                ["PORT"] = port.ToString(),
                ["grpcMaxMessageLength"] = maxMessageLength?.ToString(),
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
            GrpcClientFactoryOptions factoryOptions = monitor.Get(builder.Name);

            Assert.Equal(new Uri($"http://localhost:{port}"), factoryOptions.Address);
            Assert.Null(handler);

            if (maxMessageLength is int expectedLength)
            {
                GrpcChannelOptions channelOptions = new();
                foreach (Action<GrpcChannelOptions> action in factoryOptions.ChannelOptionsActions)
                {
                    action(channelOptions);
                }

                Assert.Equal(expectedLength, channelOptions.MaxReceiveMessageSize);
                Assert.Equal(expectedLength, channelOptions.MaxSendMessageSize);
            }
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

#endif
