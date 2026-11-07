// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#if !NET6_0_OR_GREATER
using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;
using GrpcCore = Grpc.Core;

namespace Microsoft.Azure.Functions.Worker.Grpc
{
    internal partial class GrpcWorkerClientFactory
    {
        internal partial class GrpcWorkerClient
        {
            public GrpcWorkerClient(GrpcHostChannel outputChannel, GrpcWorkerStartupOptions startupOptions, IMessageProcessor processor, ILoggerFactory loggerFactory)
            {
                _startupOptions = startupOptions ?? throw new ArgumentNullException(nameof(startupOptions));
                _processor = processor ?? throw new ArgumentNullException(nameof(processor));
                _outputReader = outputChannel.Channel.Reader;
                _outputWriter = outputChannel.Channel.Writer;
                _grpcClient = CreateClient();
            }

            private Task StartCoreAsync(CancellationToken token)
            {
                var eventStream = _grpcClient.EventStream(cancellationToken: token);
                return StartStreamAsync(eventStream);
            }

            private FunctionRpcClient CreateClient()
            {
                var options = new ChannelOption[]
                {
                    new ChannelOption(GrpcCore.ChannelOptions.MaxReceiveMessageLength, _startupOptions.GrpcMaxMessageLength),
                    new ChannelOption(GrpcCore.ChannelOptions.MaxSendMessageLength, _startupOptions.GrpcMaxMessageLength)
                };

                GrpcCore.Channel grpcChannel = new GrpcCore.Channel(_startupOptions.HostEndpoint!.Host, _startupOptions.HostEndpoint.Port, ChannelCredentials.Insecure, options);

                return new FunctionRpcClient(grpcChannel);
            }
        }
    }
}
#endif
