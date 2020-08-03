using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using MsgType = Microsoft.Azure.WebJobs.Script.Grpc.Messages.StreamingMessage.ContentOneofCase;


namespace Microsoft.Azure.Functions.DotNetWorker
{
    internal class DefaultFunctionsHostClient : IFunctionsHostClient
    {
        private readonly ChannelWriter<StreamingMessage> _outputWriter;
        private readonly IHostRequestHandler _handler;

        public DefaultFunctionsHostClient(IHostRequestHandler handler, FunctionsHostOutputChannel outputChannel)
        {
            if (outputChannel == null)
            {
                throw new ArgumentNullException(nameof(outputChannel));
            }

            _outputWriter = outputChannel.Channel.Writer;

            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task ProcessRequestAsync(StreamingMessage request)
        {
            // Dispatch and return.
            Task.Run(() => ProcessRequestCoreAsync(request));
            return Task.CompletedTask;
        }

        private Task ProcessRequestCoreAsync(StreamingMessage request)
        {
            return request.ContentCase switch
            {
                MsgType.WorkerInitRequest => WorkerInitRequestHandlerAsync(request),
                MsgType.FunctionLoadRequest => FunctionLoadRequestHandlerAsync(request),
                MsgType.InvocationRequest => InvocationRequestHandlerAsync(request),
                MsgType.FunctionEnvironmentReloadRequest => FunctionEnvironmentReloadRequestHandlerAsync(request),
                _ => Task.CompletedTask,
            };
        }

        internal async Task InvocationRequestHandlerAsync(StreamingMessage request)
        {
            InvocationResponse response = await _handler.InvokeFunctionAsync(request.InvocationRequest);

            StreamingMessage responseMessage = new StreamingMessage
            {
                RequestId = request.RequestId,
                InvocationResponse = response
            };

            await _outputWriter.WriteAsync(responseMessage);
        }

        internal async Task WorkerInitRequestHandlerAsync(StreamingMessage request)
        {
            WorkerInitResponse response = await _handler.InitializeWorkerAsync(request.WorkerInitRequest);

            StreamingMessage responseMessage = new StreamingMessage
            {
                RequestId = request.RequestId,
                WorkerInitResponse = response
            };

            await _outputWriter.WriteAsync(responseMessage);
        }

        internal async Task FunctionLoadRequestHandlerAsync(StreamingMessage request)
        {
            FunctionLoadResponse response = await _handler.LoadFunctionAsync(request.FunctionLoadRequest);

            StreamingMessage responseMessage = new StreamingMessage
            {
                RequestId = request.RequestId,
                FunctionLoadResponse = response
            };

            await _outputWriter.WriteAsync(responseMessage);
        }

        internal async Task FunctionEnvironmentReloadRequestHandlerAsync(StreamingMessage request)
        {
            FunctionEnvironmentReloadResponse response = await _handler.ReloadEnvironmentAsync(request.FunctionEnvironmentReloadRequest);

            StreamingMessage responseMessage = new StreamingMessage
            {
                RequestId = request.RequestId,
                FunctionEnvironmentReloadResponse = response
            };

            await _outputWriter.WriteAsync(responseMessage);
        }
    }
}
