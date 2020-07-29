using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.WebJobs.Script.Grpc.Messages.FunctionRpc;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    internal class WorkerHostedService : IHostedService
    {
        private readonly WorkerStartupOptions _options;
        private readonly FunctionRpcClient _rpcClient;
        private readonly FunctionsHostChannelManager _channelManager;
        private readonly IFunctionsHostClient _client;

        private Task? _writerTask;
        private Task? _readerTask;

        public WorkerHostedService(FunctionRpcClient rpcClient, FunctionsHostChannelManager channelManager, IFunctionsHostClient client, IOptions<WorkerStartupOptions> options)
        {
            _rpcClient = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));
            _channelManager = channelManager ?? throw new ArgumentNullException(nameof(channelManager));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var eventStream = _rpcClient.EventStream();

            _writerTask = StartWriterAsync(eventStream.RequestStream);
            _readerTask = StartReaderAsync(eventStream.ResponseStream);

            await SendStartStreamMessage(eventStream.RequestStream);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // TODO: Proper handling of shutdown.
            return Task.CompletedTask;
        }

        public async Task SendStartStreamMessage(IClientStreamWriter<StreamingMessage> requestStream)
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
            await foreach (StreamingMessage rpcWriteMsg in _channelManager.OutputChannel.Reader.ReadAllAsync())
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
