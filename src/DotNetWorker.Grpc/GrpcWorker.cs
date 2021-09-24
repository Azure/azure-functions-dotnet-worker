// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Features;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Azure.Functions.Worker.Rpc;
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
        private readonly IInputConversionFeatureProvider _inputConversionFeatureProvider;
        private readonly IMethodInfoLocator _methodInfoLocator;
        private readonly IOptions<GrpcWorkerStartupOptions> _startupOptions;
        private readonly ObjectSerializer _serializer;

        public GrpcWorker(IFunctionsApplication application, FunctionRpcClient rpcClient, GrpcHostChannel outputChannel, IInvocationFeaturesFactory invocationFeaturesFactory,
            IOutputBindingsInfoProvider outputBindingsInfoProvider, IMethodInfoLocator methodInfoLocator, 
            IOptions<GrpcWorkerStartupOptions> startupOptions, IOptions<WorkerOptions> workerOptions,
            IInputConversionFeatureProvider inputConversionFeatureProvider)
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
            _serializer = workerOptions.Value.Serializer ?? throw new InvalidOperationException(nameof(workerOptions.Value.Serializer));
            _inputConversionFeatureProvider = inputConversionFeatureProvider ?? throw new ArgumentNullException(nameof(inputConversionFeatureProvider));
        }

        public async Task StartAsync(CancellationToken token)
        {
            var eventStream = _rpcClient.EventStream(cancellationToken: token);

            await SendStartStreamMessageAsync(eventStream.RequestStream);

            _ = StartWriterAsync(eventStream.RequestStream);
            _ = StartReaderAsync(eventStream.ResponseStream);
        }

        public Task StopAsync(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        private async Task SendStartStreamMessageAsync(IClientStreamWriter<StreamingMessage> requestStream)
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

        private async Task StartWriterAsync(IClientStreamWriter<StreamingMessage> requestStream)
        {
            await foreach (StreamingMessage rpcWriteMsg in _outputReader.ReadAllAsync())
            {
                await requestStream.WriteAsync(rpcWriteMsg);
            }
        }

        private async Task StartReaderAsync(IAsyncStreamReader<StreamingMessage> responseStream)
        {
            while (await responseStream.MoveNext())
            {
                await ProcessRequestAsync(responseStream.Current);
            }
        }

        private Task ProcessRequestAsync(StreamingMessage request)
        {
            // Dispatch and return.
            Task.Run(() => ProcessRequestCoreAsync(request));
            return Task.CompletedTask;
        }

        private async Task ProcessRequestCoreAsync(StreamingMessage request)
        {
            StreamingMessage responseMessage = new StreamingMessage
            {
                RequestId = request.RequestId
            };

            if (request.ContentCase == MsgType.InvocationRequest)
            {
                responseMessage.InvocationResponse = await InvocationRequestHandlerAsync(request.InvocationRequest, _application,
                    _invocationFeaturesFactory, _serializer, _outputBindingsInfoProvider, _inputConversionFeatureProvider);
            }
            else if (request.ContentCase == MsgType.WorkerInitRequest)
            {
                responseMessage.WorkerInitResponse = WorkerInitRequestHandler(request.WorkerInitRequest);
            }
            else if (request.ContentCase == MsgType.FunctionLoadRequest)
            {
                responseMessage.FunctionLoadResponse = FunctionLoadRequestHandler(request.FunctionLoadRequest, _application, _methodInfoLocator);
            }
            else if (request.ContentCase == MsgType.FunctionEnvironmentReloadRequest)
            {
                // No-op for now, but return a response.
                responseMessage.FunctionEnvironmentReloadResponse = new FunctionEnvironmentReloadResponse
                {
                    Result = new StatusResult { Status = StatusResult.Types.Status.Success }
                };
            }
            else
            {
                // TODO: Trace failure here.
                return;
            }

            await _outputWriter.WriteAsync(responseMessage);
        }

        internal static async Task<InvocationResponse> InvocationRequestHandlerAsync(InvocationRequest request, IFunctionsApplication application,
            IInvocationFeaturesFactory invocationFeaturesFactory, ObjectSerializer serializer, 
            IOutputBindingsInfoProvider outputBindingsInfoProvider,
            IInputConversionFeatureProvider functionInputConversionFeatureProvider)
        {
            FunctionContext? context = null;
            InvocationResponse response = new()
            {
                InvocationId = request.InvocationId
            };

            try
            {
                var invocation = new GrpcFunctionInvocation(request);

                IInvocationFeatures invocationFeatures = invocationFeaturesFactory.Create();
                invocationFeatures.Set<FunctionInvocation>(invocation);
                invocationFeatures.Set<IExecutionRetryFeature>(invocation);

                context = application.CreateContext(invocationFeatures);
                invocationFeatures.Set<IFunctionBindingsFeature>(new GrpcFunctionBindingsFeature(context, request, outputBindingsInfoProvider));

                if (functionInputConversionFeatureProvider.TryCreate(typeof(DefaultInputConversionFeature), out var conversion))
                {                                    
                    invocationFeatures.Set<IInputConversionFeature>(conversion!);
                }

                await application.InvokeFunctionAsync(context);

                var functionBindings = context.GetBindings();

                foreach (var binding in functionBindings.OutputBindingData)
                {
                    var parameterBinding = new ParameterBinding
                    {
                        Name = binding.Key
                    };

                    if (binding.Value is not null)
                    {
                        parameterBinding.Data = await binding.Value.ToRpcAsync(serializer);
                    }

                    response.OutputData.Add(parameterBinding);
                }

                if (functionBindings.InvocationResult != null)
                {
                    TypedData? returnVal = await functionBindings.InvocationResult.ToRpcAsync(serializer);

                    response.ReturnValue = returnVal;
                }

                response.Result = new StatusResult
                {
                    Status = StatusResult.Types.Status.Success
                };
            }
            catch (Exception ex)
            {
                response.Result = new StatusResult
                {
                    Exception = ex.ToRpcException(),
                    Status = StatusResult.Types.Status.Failure
                };
            }
            finally
            {
                (context as IDisposable)?.Dispose();
            }

            return response;
        }

        internal static WorkerInitResponse WorkerInitRequestHandler(WorkerInitRequest request)
        {
            var response = new WorkerInitResponse
            {
                Result = new StatusResult { Status = StatusResult.Types.Status.Success },
                WorkerVersion = WorkerInformation.Instance.WorkerVersion
            };

            response.Capabilities.Add("RpcHttpBodyOnly", bool.TrueString);
            response.Capabilities.Add("RawHttpBodyBytes", bool.TrueString);
            response.Capabilities.Add("RpcHttpTriggerMetadataRemoved", bool.TrueString);
            response.Capabilities.Add("UseNullableValueDictionaryForHttp", bool.TrueString);
            response.Capabilities.Add("TypedDataCollection", bool.TrueString);

            return response;
        }

        internal static FunctionLoadResponse FunctionLoadRequestHandler(FunctionLoadRequest request, IFunctionsApplication application, IMethodInfoLocator methodInfoLocator)
        {
            var response = new FunctionLoadResponse
            {
                FunctionId = request.FunctionId,
                Result = StatusResult.Success
            };

            if (!request.Metadata.IsProxy)
            {
                try
                {
                    FunctionDefinition definition = request.ToFunctionDefinition(methodInfoLocator);
                    application.LoadFunction(definition);
                }
                catch (Exception ex)
                {
                    response.Result = new StatusResult
                    {
                        Status = StatusResult.Types.Status.Failure,
                        Exception = ex.ToRpcException()
                    };
                }
            }

            return response;
        }
    }
}
