using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using MsgType = Microsoft.Azure.WebJobs.Script.Grpc.Messages.StreamingMessage.ContentOneofCase;


namespace Microsoft.Azure.Functions.DotNetWorker
{
    internal class RxFunctionsHostClient : IFunctionsHostClient
    {
        private readonly IHostRequestHandler _handler;
        private readonly ScriptEventManager _eventManager;
        private readonly FunctionsHostChannelWriter _channelWriter;
        private readonly IObservable<InboundEvent> _inboundWorkerEvents;
        private readonly List<IDisposable> _eventSubscriptions = new List<IDisposable>();

        public RxFunctionsHostClient(IHostRequestHandler handler, FunctionsHostChannelWriter channelWriter)
        {
            _handler = handler;

            _eventManager = new ScriptEventManager();

            _channelWriter = channelWriter;

            _inboundWorkerEvents = _eventManager.OfType<InboundEvent>();

            _eventSubscriptions.Add(_inboundWorkerEvents.Where(msg => msg.MessageType == MsgType.InvocationRequest)
                .Subscribe((msg) => InvocationRequestHandler(msg.Message)));

            _eventSubscriptions.Add(_inboundWorkerEvents.Where(msg => msg.MessageType == MsgType.WorkerInitRequest)
                .Subscribe((msg) => WorkerInitRequestHandler(msg.Message)));

            _eventSubscriptions.Add(_inboundWorkerEvents.Where(msg => msg.MessageType == MsgType.FunctionLoadRequest)
               .Subscribe((msg) => FunctionLoadRequestHandler(msg.Message)));

            _eventSubscriptions.Add(_inboundWorkerEvents.Where(msg => msg.MessageType == MsgType.FunctionEnvironmentReloadRequest)
               .Subscribe((msg) => FunctionEnvironmentReloadRequestHandler(msg.Message)));
        }

        public Task ProcessRequestAsync(StreamingMessage request)
        {
            _eventManager.Publish(new InboundEvent("na", request));
            return Task.CompletedTask;
        }

        internal void InvocationRequestHandler(StreamingMessage request)
        {
            InvocationResponse response = _handler.InvokeFunctionAsync(request.InvocationRequest).Result;

            StreamingMessage responseMessage = new StreamingMessage
            {
                RequestId = request.RequestId,
                InvocationResponse = response
            };

            _channelWriter.Writer.TryWrite(responseMessage);
        }

        internal void WorkerInitRequestHandler(StreamingMessage request)
        {
            WorkerInitResponse response = _handler.InitializeWorkerAsync(request.WorkerInitRequest).Result;

            StreamingMessage responseMessage = new StreamingMessage
            {
                RequestId = request.RequestId,
                WorkerInitResponse = response
            };

            _channelWriter.Writer.TryWrite(responseMessage);
        }

        internal void FunctionLoadRequestHandler(StreamingMessage request)
        {
            FunctionLoadResponse response = _handler.LoadFunctionAsync(request.FunctionLoadRequest).Result;

            StreamingMessage responseMessage = new StreamingMessage
            {
                RequestId = request.RequestId,
                FunctionLoadResponse = response
            };

            _channelWriter.Writer.TryWrite(responseMessage);
        }

        internal void FunctionEnvironmentReloadRequestHandler(StreamingMessage request)
        {
            FunctionEnvironmentReloadResponse response = _handler.ReloadEnvironmentAsync(request.FunctionEnvironmentReloadRequest).Result;

            StreamingMessage responseMessage = new StreamingMessage
            {
                RequestId = request.RequestId,
                FunctionEnvironmentReloadResponse = response
            };

            _channelWriter.Writer.TryWrite(responseMessage);
        }
    }
}
