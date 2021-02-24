// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;

namespace Microsoft.Azure.Functions.Worker
{
    internal class WorkerHostedService : IHostedService
    {
        private readonly WorkerStartupOptions _options;
        private readonly FunctionRpcClient _rpcClient;
        private readonly FunctionsHostOutputChannel _outputChannel;
        private readonly IFunctionsHostClient _client;

        private Task? _writerTask;
        private Task? _readerTask;

        public WorkerHostedService(FunctionRpcClient rpcClient, FunctionsHostOutputChannel outputChannel, IFunctionsHostClient client, IOptions<WorkerStartupOptions> options)
        {
            _rpcClient = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));
            _outputChannel = outputChannel ?? throw new ArgumentNullException(nameof(outputChannel));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var eventStream = _rpcClient.EventStream();

            _writerTask = StartWriterAsync(eventStream.RequestStream);
            _readerTask = StartReaderAsync(eventStream.ResponseStream);

            await SendStartStreamMessageAsync(eventStream.RequestStream);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // TODO: Proper handling of shutdown.
            return Task.CompletedTask;
        }

        public async Task SendStartStreamMessageAsync(IClientStreamWriter<StreamingMessage> requestStream)
        {
            StartStream str = new StartStream()
            {
                WorkerId = _options.WorkerId
            };

            StreamingMessage startStream = new StreamingMessage()
            {
                StartStream = str
            };

            await requestStream.WriteAsync(startStream);
        }

        public async Task StartWriterAsync(IClientStreamWriter<StreamingMessage> requestStream)
        {
            await foreach (StreamingMessage rpcWriteMsg in _outputChannel.Channel.Reader.ReadAllAsync())
            {
                await requestStream.WriteAsync(rpcWriteMsg);
            }
        }

        public async Task StartReaderAsync(IAsyncStreamReader<StreamingMessage> responseStream)
        {
            while (await responseStream.MoveNext())
            {
                await _client.ProcessRequestAsync(responseStream.Current);
            }
        }
    }
}
