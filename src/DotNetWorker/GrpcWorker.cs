﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker.Context;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;
using MsgType = Microsoft.Azure.Functions.Worker.Grpc.Messages.StreamingMessage.ContentOneofCase;


namespace Microsoft.Azure.Functions.Worker
{
    internal class GrpcWorker : IWorker
    {
        private readonly ChannelReader<StreamingMessage> _outputReader;
        private readonly ChannelWriter<StreamingMessage> _outputWriter;

        private readonly IFunctionsApplication _application;
        private readonly FunctionRpcClient _rpcClient;
        private readonly IInvocationFeaturesFactory _invocationFeaturesFactory;
        private readonly IOutputBindingsInfoProvider _outputBindingsInfoProvider;
        private readonly IMethodInfoLocator _methodInfoLocator;
        private readonly IOptions<GrpcWorkerStartupOptions> _startupOptions;
        private Task? _writerTask;
        private Task? _readerTask;

        public GrpcWorker(IFunctionsApplication application, FunctionRpcClient rpcClient, FunctionsHostOutputChannel outputChannel, IInvocationFeaturesFactory invocationFeaturesFactory,
            IOutputBindingsInfoProvider outputBindingsInfoProvider, IMethodInfoLocator methodInfoLocator, IOptions<GrpcWorkerStartupOptions> startupOptions)
        {
            if (outputChannel == null)
            {
                throw new ArgumentNullException(nameof(outputChannel));
            }

            _outputReader = outputChannel.Channel.Reader;
            _outputWriter = outputChannel.Channel.Writer;

            _application = application ?? throw new ArgumentNullException(nameof(application));
            _rpcClient = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));
            _invocationFeaturesFactory = invocationFeaturesFactory ?? throw new ArgumentNullException(nameof(invocationFeaturesFactory));
            _outputBindingsInfoProvider = outputBindingsInfoProvider ?? throw new ArgumentNullException(nameof(outputBindingsInfoProvider));
            _methodInfoLocator = methodInfoLocator ?? throw new ArgumentNullException(nameof(methodInfoLocator));
            _startupOptions = startupOptions ?? throw new ArgumentNullException(nameof(startupOptions));
        }

        public Task StartAsync(CancellationToken token)
        {
            var eventStream = _rpcClient.EventStream();

            _writerTask = StartWriterAsync(eventStream.RequestStream);
            _readerTask = StartReaderAsync(eventStream.ResponseStream);

            return SendStartStreamMessageAsync(eventStream.RequestStream);
        }

        public Task StopAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public async Task SendStartStreamMessageAsync(IClientStreamWriter<StreamingMessage> requestStream)
        {
            StartStream str = new StartStream()
            {
                WorkerId = _startupOptions.Value.WorkerId
            };

            StreamingMessage startStream = new StreamingMessage()
            {
                StartStream = str
            };

            await requestStream.WriteAsync(startStream);
        }

        public async Task StartWriterAsync(IClientStreamWriter<StreamingMessage> requestStream)
        {
            await foreach (StreamingMessage rpcWriteMsg in _outputReader.ReadAllAsync())
            {
                await requestStream.WriteAsync(rpcWriteMsg);
            }
        }

        public async Task StartReaderAsync(IAsyncStreamReader<StreamingMessage> responseStream)
        {
            while (await responseStream.MoveNext())
            {
                await ProcessRequestAsync(responseStream.Current);
            }
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
                // TODO: Trace that we missed this MsgType
                _ => Task.CompletedTask,
            };
        }

        internal async Task InvocationRequestHandlerAsync(StreamingMessage request)
        {
            var invocation = new GrpcFunctionInvocation(request.InvocationRequest);

            IInvocationFeatures invocationFeatures = _invocationFeaturesFactory.Create();
            invocationFeatures.Set<FunctionInvocation>(invocation);

            FunctionContext context = _application.CreateContext(invocationFeatures);

            invocationFeatures.Set<IFunctionBindingsFeature>(new GrpcFunctionBindingsFeature(context, request.InvocationRequest, _outputBindingsInfoProvider));

            InvocationResponse response = await _application.InvokeFunctionAsync(context);

            StreamingMessage responseMessage = new StreamingMessage
            {
                RequestId = request.RequestId,
                InvocationResponse = response
            };

            await _outputWriter.WriteAsync(responseMessage);
        }

        internal async Task WorkerInitRequestHandlerAsync(StreamingMessage request)
        {
            WorkerInitResponse response = await _application.InitializeWorkerAsync(request.WorkerInitRequest);

            StreamingMessage responseMessage = new StreamingMessage
            {
                RequestId = request.RequestId,
                WorkerInitResponse = response
            };

            await _outputWriter.WriteAsync(responseMessage);
        }

        internal async Task FunctionLoadRequestHandlerAsync(StreamingMessage request)
        {
            FunctionLoadRequest loadRequest = request.FunctionLoadRequest;

            if (!loadRequest.Metadata.IsProxy)
            {
                FunctionDefinition definition = loadRequest.ToFunctionDefinition(_methodInfoLocator);
                _application.LoadFunction(definition);
            }

            var response = new FunctionLoadResponse
            {
                FunctionId = loadRequest.FunctionId,
                Result = new StatusResult { Status = StatusResult.Types.Status.Success }
            };

            var responseMessage = new StreamingMessage
            {
                RequestId = request.RequestId,
                FunctionLoadResponse = response
            };

            await _outputWriter.WriteAsync(responseMessage);
        }

        internal async Task FunctionEnvironmentReloadRequestHandlerAsync(StreamingMessage request)
        {
            FunctionEnvironmentReloadResponse response = await _application.ReloadEnvironmentAsync(request.FunctionEnvironmentReloadRequest);

            StreamingMessage responseMessage = new StreamingMessage
            {
                RequestId = request.RequestId,
                FunctionEnvironmentReloadResponse = response
            };

            await _outputWriter.WriteAsync(responseMessage);
        }
    }
}
