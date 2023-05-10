using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace FunctionsNetHost
{
    internal class InboundMessageChannel
    {
        private Channel<StreamingMessage> _inboundChannel;

        private static readonly InboundMessageChannel instance = new InboundMessageChannel();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static InboundMessageChannel()
        {
        }

        private InboundMessageChannel()
        {
        }

        public static InboundMessageChannel Instance
        {
            get
            {
                return instance;
            }
        }

        public Channel<StreamingMessage> InboundChannel { get { return _inboundChannel; } }
    }

    internal class IncomingMessageHandler
    {
        private Channel<StreamingMessage> outgoingMessageChannel;

        public IncomingMessageHandler(Channel<StreamingMessage> outgoingMessageChannel)
        {
            this.outgoingMessageChannel = outgoingMessageChannel;
        }

        internal Task ProcessMessageAsync(StreamingMessage message)
        {
            Task.Run(() =>
            {
                Process(message);
            });

            return Task.CompletedTask;
        }

        private async Task Process(StreamingMessage msg)
        {
            Logger.Log($"New message received in client:{msg.ContentCase}");

            StreamingMessage responseMessage = new StreamingMessage
            {
            };

            if (msg.ContentCase == StreamingMessage.ContentOneofCase.WorkerInitRequest)
            {
                var response = BuildWorkerInitResponse();
                responseMessage.WorkerInitResponse = response;
            }
            if (msg.ContentCase == StreamingMessage.ContentOneofCase.FunctionsMetadataRequest)
            {
               
                var response = new FunctionMetadataResponse {  UseDefaultMetadataIndexing = true , Result = new StatusResult { Status = StatusResult.Types.Status.Success } };
                responseMessage.FunctionMetadataResponse = response;
            }
            else if (msg.ContentCase == StreamingMessage.ContentOneofCase.FunctionEnvironmentReloadRequest)
            {
                var exePath = msg.FunctionEnvironmentReloadRequest.FunctionAppDirectory;
                Logger.Log($"exePath: {exePath}");

                // load customer assembly
                // wait until we get a signal that it is loaded.
                await InboundMessageChannel.Instance.InboundChannel.Writer.WriteAsync(msg);
            }

            await outgoingMessageChannel.Writer.WriteAsync(responseMessage);
        }

        private static WorkerInitResponse BuildWorkerInitResponse()
        {
            var response = new WorkerInitResponse
            {
                Result = new StatusResult { Status = StatusResult.Types.Status.Success }
            };

            return response;
        }
    }
}
