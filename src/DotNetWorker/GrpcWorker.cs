// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Context;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.StatusResult.Types;
using MsgType = Microsoft.Azure.Functions.Worker.Grpc.Messages.StreamingMessage.ContentOneofCase;


namespace Microsoft.Azure.Functions.Worker
{
    internal class GrpcWorker : IWorker
    {
        private readonly ChannelWriter<StreamingMessage> _outputWriter;
        private readonly IFunctionsApplication _application;
        private readonly IInvocationFeaturesFactory _invocationFeaturesFactory;
        private readonly IOutputBindingsInfoProvider _outputBindingsInfoProvider;
        private readonly IMethodInfoLocator _methodInfoLocator;

        public GrpcWorker(IFunctionsApplication application, FunctionsHostOutputChannel outputChannel, IInvocationFeaturesFactory invocationFeaturesFactory,
            IOutputBindingsInfoProvider outputBindingsInfoProvider, IMethodInfoLocator methodInfoLocator)
        {
            if (outputChannel == null)
            {
                throw new ArgumentNullException(nameof(outputChannel));
            }

            _outputWriter = outputChannel.Channel.Writer;

            _application = application ?? throw new ArgumentNullException(nameof(application));
            _invocationFeaturesFactory = invocationFeaturesFactory ?? throw new ArgumentNullException(nameof(invocationFeaturesFactory));
            _outputBindingsInfoProvider = outputBindingsInfoProvider ?? throw new ArgumentNullException(nameof(outputBindingsInfoProvider));
            _methodInfoLocator = methodInfoLocator ?? throw new ArgumentNullException(nameof(methodInfoLocator));
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
                Result = new StatusResult { Status = Status.Success }
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
