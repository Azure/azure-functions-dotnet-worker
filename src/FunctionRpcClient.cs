// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using System.Xml;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using MsgType = Microsoft.Azure.WebJobs.Script.Grpc.Messages.StreamingMessage.ContentOneofCase;
using System.Text.Json;
using FunctionsDotNetWorker.Converters;
using FunctionsDotNetWorker.Logging;

namespace FunctionsDotNetWorker
{
    public class FunctionRpcClient
    {
        private readonly FunctionRpc.FunctionRpcClient client;
        private readonly IScriptEventManager _eventManager;
        private string _workerId;
        private IObservable<InboundEvent> _inboundWorkerEvents;
        IDictionary<string, IDisposable> _outboundEventSubscriptions = new Dictionary<string, IDisposable>();
        private List<IDisposable> _eventSubscriptions = new List<IDisposable>();
        private ConcurrentBag<StreamingMessage> invokeRes = new ConcurrentBag<StreamingMessage>();
        private BlockingCollection<StreamingMessage> _blockingCollectionQueue = new BlockingCollection<StreamingMessage>();
        private readonly AsyncDuplexStreamingCall<StreamingMessage, StreamingMessage> _call;
        IClientStreamWriter<StreamingMessage> _requestStream;
        FunctionBroker _functionBroker;
        WorkerLogManager _workerLogManager;

        public FunctionRpcClient(FunctionRpc.FunctionRpcClient client, string workerId, FunctionBroker functionBroker, WorkerLogManager workerLogManager)
        {
            this.client = client;
            _call = client.EventStream();
            _workerId = workerId;
            _eventManager = new ScriptEventManager();
            _functionBroker = functionBroker;
            _workerLogManager = workerLogManager;
            _workerLogManager.AddBlockingQueue(_blockingCollectionQueue);

            _inboundWorkerEvents = _eventManager.OfType<InboundEvent>()
                .Where(msg => msg.WorkerId == _workerId);

            _eventSubscriptions.Add(_inboundWorkerEvents.Where(msg => msg.MessageType == MsgType.InvocationRequest)
                .Subscribe((msg) => InvocationRequestHandler(msg.Message)));

            _eventSubscriptions.Add(_inboundWorkerEvents.Where(msg => msg.MessageType == MsgType.WorkerInitRequest)
                .Subscribe((msg) => WorkerInitRequestHandler(msg.Message)));

            _eventSubscriptions.Add(_inboundWorkerEvents.Where(msg => msg.MessageType == MsgType.WorkerTerminate)
               .Subscribe((msg) => WorkerTerminateRequest(msg.Message)));

            _eventSubscriptions.Add(_inboundWorkerEvents.Where(msg => msg.MessageType == MsgType.FunctionLoadRequest)
               .Subscribe((msg) => FunctionLoadRequestHandler(msg.Message)));

            _eventSubscriptions.Add(_inboundWorkerEvents.Where(msg => msg.MessageType == MsgType.FunctionEnvironmentReloadRequest)
               .Subscribe((msg) => FunctionEnvironmentReloadRequestHandler(msg.Message)));
        }

        private StreamingMessage NewStreamingMessageTemplate(string requestId, StreamingMessage.ContentOneofCase msgType, out StatusResult status)
        {
            // Assume success. The state of the status object can be changed in the caller.
            status = new StatusResult() { Status = StatusResult.Types.Status.Success };
            var response = new StreamingMessage() { RequestId = requestId };

            switch (msgType)
            {
                case StreamingMessage.ContentOneofCase.WorkerInitResponse:
                    response.WorkerInitResponse = new WorkerInitResponse() { Result = status };
                    break;
                case StreamingMessage.ContentOneofCase.WorkerStatusResponse:
                    response.WorkerStatusResponse = new WorkerStatusResponse();
                    break;
                case StreamingMessage.ContentOneofCase.FunctionLoadResponse:
                    response.FunctionLoadResponse = new FunctionLoadResponse() { Result = status };
                    break;
                case StreamingMessage.ContentOneofCase.InvocationResponse:
                    response.InvocationResponse = new InvocationResponse() { Result = status };
                    break;
                case StreamingMessage.ContentOneofCase.FunctionEnvironmentReloadResponse:
                    response.FunctionEnvironmentReloadResponse = new FunctionEnvironmentReloadResponse() { Result = status };
                    break;
                default:
                    throw new InvalidOperationException("Unreachable code.");
            }

            return response;
        }

        internal void WorkerInitRequestHandler(StreamingMessage request)
        {
            StreamingMessage response = NewStreamingMessageTemplate(
                request.RequestId,
                StreamingMessage.ContentOneofCase.WorkerInitResponse,
                out StatusResult status);
            _blockingCollectionQueue.Add(response);
        }

        internal Task<StreamingMessage> WorkerTerminateRequest(StreamingMessage request)
        {
            return null;
        }

        internal void FunctionLoadRequestHandler(StreamingMessage request)
        {
            FunctionLoadRequest functionLoadRequest = request.FunctionLoadRequest;

            if (!string.IsNullOrEmpty(functionLoadRequest?.Metadata?.ScriptFile))
            {
                _functionBroker.AddFunction(functionLoadRequest);
            }

            StreamingMessage response = NewStreamingMessageTemplate(
                request.RequestId,
                StreamingMessage.ContentOneofCase.FunctionLoadResponse,
                out StatusResult status);
            response.FunctionLoadResponse.FunctionId = functionLoadRequest.FunctionId;
            _blockingCollectionQueue.Add(response);

        }

        internal void InvocationRequestHandler(StreamingMessage request)
        {
            StreamingMessage response = NewStreamingMessageTemplate(
                    request.RequestId,
                    MsgType.InvocationResponse,
                    out StatusResult status);
            response.InvocationResponse.InvocationId = request.InvocationRequest.InvocationId;
            
            try
            {
                List<ParameterBinding> parameterBindings;
                var result = _functionBroker.Invoke(request.InvocationRequest, out parameterBindings, _workerLogManager);

                foreach(var paramBinding in parameterBindings)
                {
                    response.InvocationResponse.OutputData.Add(paramBinding);
                }
                if (result != null)
                {
                    var returnVal = ToRpcConverter.ToRpc(result);

                    response.InvocationResponse.ReturnValue = returnVal;
                }
            }
            catch (Exception)
            {
                status.Status = StatusResult.Types.Status.Failure;

                // failure + cancellation might need to be separated 
            }
            
            _blockingCollectionQueue.Add(response);
        }

        internal void FunctionEnvironmentReloadRequestHandler(StreamingMessage request)
        {
            StreamingMessage response = NewStreamingMessageTemplate(
                request.RequestId,
                StreamingMessage.ContentOneofCase.FunctionEnvironmentReloadResponse,
                out StatusResult status);
            _blockingCollectionQueue.Add(response);
        }

        public async Task<bool> RpcStream()
        {
            StartStream str = new StartStream()
            {
                WorkerId = _workerId
            };
            StreamingMessage startStream = new StreamingMessage()
            {
                StartStream = str
            };
            await _call.RequestStream.WriteAsync(startStream);
            var consumer = Task.Run(async () =>
            {
                foreach (var rpcWriteMsg in _blockingCollectionQueue.GetConsumingEnumerable())
                {
                    await _call.RequestStream.WriteAsync(rpcWriteMsg);
                }
            });
            await consumer;
            return true;
        }

        public async Task RpcStreamReader()
        {
            while (await _call.ResponseStream.MoveNext())
            {
                var serverMessage = _call.ResponseStream.Current;
                _eventManager.Publish(new InboundEvent(_workerId, serverMessage));
            }
        }
    }
}
