using System.Threading.Channels;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;

namespace FunctionsNetHost
{
    internal class MyClient
    {
        public Channel<StreamingMessage> _outgoingMessageChannel;

        private IncomingMessageHandler _processor;
        private GrpcWorkerStartupOptions grpcWorkerStartupOptions;

        public MyClient(GrpcWorkerStartupOptions grpcWorkerStartupOptions)
        {
            this.grpcWorkerStartupOptions = grpcWorkerStartupOptions;
            UnboundedChannelOptions outputOptions = new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false,
                AllowSynchronousContinuations = true
            };

            _outgoingMessageChannel = Channel.CreateUnbounded<StreamingMessage>(outputOptions);

            _processor = new IncomingMessageHandler(_outgoingMessageChannel);
        }

        public async Task InitAsync()
        {
            var endpoint = $"http://{grpcWorkerStartupOptions.Host}:{grpcWorkerStartupOptions.Port}";
            Logger.Log($"Grpc server endpoint:{endpoint}");

            var client = CreateClient(endpoint);

            var eventStream = client.EventStream(cancellationToken: CancellationToken.None);

            await SendStartStreamMessageAsync(eventStream.RequestStream);

            var r = StartReaderAsync(eventStream.ResponseStream);
            var w = StartWriterAsync(eventStream.RequestStream);

            await Task.WhenAll(r, w);
        }

        public async Task SendAsync(StreamingMessage message)
        {
            await _outgoingMessageChannel.Writer.WriteAsync(message);
        }

        private async Task StartReaderAsync(IAsyncStreamReader<StreamingMessage> responseStream)
        {
            while (await responseStream.MoveNext())
            {
                await _processor!.ProcessMessageAsync(responseStream.Current);
            }
        }
        private async Task StartWriterAsync(IClientStreamWriter<StreamingMessage> requestStream)
        {
            await foreach (StreamingMessage rpcWriteMsg in _outgoingMessageChannel.Reader.ReadAllAsync())
            {
                Logger.Log("Outgoing message : " + rpcWriteMsg.ContentCase);
                await requestStream.WriteAsync(rpcWriteMsg);
            }
        }

        private async Task SendStartStreamMessageAsync(IClientStreamWriter<StreamingMessage> requestStream)
        {
            StartStream str = new StartStream()
            {
                WorkerId = grpcWorkerStartupOptions.WorkerId
            };

            StreamingMessage startStream = new StreamingMessage()
            {
                StartStream = str
            };

            await requestStream.WriteAsync(startStream);
        }
        private FunctionRpcClient CreateClient(string endpoint)
        {
            string uriString = endpoint;
            if (!Uri.TryCreate(uriString, UriKind.Absolute, out Uri? grpcUri))
            {
                throw new InvalidOperationException($"The gRPC channel URI '{uriString}' could not be parsed.");
            }

            GrpcChannel grpcChannel = GrpcChannel.ForAddress(grpcUri, new GrpcChannelOptions()
            {
                MaxReceiveMessageSize = grpcWorkerStartupOptions.GrpcMaxMessageLength,
                MaxSendMessageSize = grpcWorkerStartupOptions.GrpcMaxMessageLength,
                Credentials = ChannelCredentials.Insecure
            });

            return new FunctionRpcClient(grpcChannel);
        }
    }
}
