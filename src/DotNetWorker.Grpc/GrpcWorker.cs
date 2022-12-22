// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Handlers;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Azure.Functions.Worker.Rpc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private readonly GrpcWorkerStartupOptions _startupOptions;
        private readonly WorkerOptions _workerOptions;
        private readonly ObjectSerializer _serializer;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IInvocationHandler _invocationHandler;
        private readonly IFunctionMetadataProvider _functionMetadataProvider;

        public GrpcWorker(IFunctionsApplication application, FunctionRpcClient rpcClient, GrpcHostChannel outputChannel, IInvocationFeaturesFactory invocationFeaturesFactory,
            IOutputBindingsInfoProvider outputBindingsInfoProvider, IMethodInfoLocator methodInfoLocator,
            IOptions<GrpcWorkerStartupOptions> startupOptions, IOptions<WorkerOptions> workerOptions,
            IInputConversionFeatureProvider inputConversionFeatureProvider,
            IFunctionMetadataProvider functionMetadataProvider,
            IHostApplicationLifetime hostApplicationLifetime,
            ILogger<GrpcWorker> logger)
        {
            if (outputChannel == null)
            {
                throw new ArgumentNullException(nameof(outputChannel));
            }

            _outputReader = outputChannel.Channel.Reader;
            _outputWriter = outputChannel.Channel.Writer;

            _hostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
            _application = application ?? throw new ArgumentNullException(nameof(application));
            _rpcClient = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));
            _invocationFeaturesFactory = invocationFeaturesFactory ?? throw new ArgumentNullException(nameof(invocationFeaturesFactory));
            _outputBindingsInfoProvider = outputBindingsInfoProvider ?? throw new ArgumentNullException(nameof(outputBindingsInfoProvider));
            _methodInfoLocator = methodInfoLocator ?? throw new ArgumentNullException(nameof(methodInfoLocator));
            _startupOptions = startupOptions?.Value ?? throw new ArgumentNullException(nameof(startupOptions));
            _workerOptions = workerOptions?.Value ?? throw new ArgumentNullException(nameof(workerOptions));
            _serializer = workerOptions.Value.Serializer ?? throw new InvalidOperationException(nameof(workerOptions.Value.Serializer));
            _inputConversionFeatureProvider = inputConversionFeatureProvider ?? throw new ArgumentNullException(nameof(inputConversionFeatureProvider));
            _functionMetadataProvider = functionMetadataProvider ?? throw new ArgumentNullException(nameof(functionMetadataProvider));

            // Handlers (TODO: dependency inject handlers instead of creating here)
            _invocationHandler = new InvocationHandler(_application, _invocationFeaturesFactory, _serializer, _outputBindingsInfoProvider, _inputConversionFeatureProvider, logger);
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
                WorkerId = _startupOptions.WorkerId
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

            switch (request.ContentCase)
            {
                case MsgType.InvocationRequest:
                    responseMessage.InvocationResponse = await InvocationRequestHandlerAsync(request.InvocationRequest);
                    break;

                case MsgType.WorkerInitRequest:
                    responseMessage.WorkerInitResponse = WorkerInitRequestHandler(request.WorkerInitRequest, _workerOptions);
                    break;

                case MsgType.WorkerStatusRequest:
                    responseMessage.WorkerStatusResponse = new WorkerStatusResponse();
                    break;

                case MsgType.FunctionsMetadataRequest:
                    responseMessage.FunctionMetadataResponse = await GetFunctionMetadataAsync(request.FunctionsMetadataRequest.FunctionAppDirectory);
                    break;

                case MsgType.WorkerTerminate:
                    WorkerTerminateRequestHandler(request.WorkerTerminate);
                    break;

                case MsgType.FunctionLoadRequest:
                    responseMessage.FunctionLoadResponse = FunctionLoadRequestHandler(request.FunctionLoadRequest, _application, _methodInfoLocator);
                    break;

                case MsgType.FunctionEnvironmentReloadRequest:
                    // No-op for now, but return a response.
                    responseMessage.FunctionEnvironmentReloadResponse = new FunctionEnvironmentReloadResponse
                    {
                        Result = new StatusResult { Status = StatusResult.Types.Status.Success }
                    };
                    break;

                case MsgType.InvocationCancel:
                    InvocationCancelRequestHandler(request.InvocationCancel);
                    break;

                default:
                    // TODO: Trace failure here.
                    return;
            }

            await _outputWriter.WriteAsync(responseMessage);
        }

        internal Task<InvocationResponse> InvocationRequestHandlerAsync(InvocationRequest request)
        {
            return _invocationHandler.InvokeAsync(request, _workerOptions);
        }

        internal void InvocationCancelRequestHandler(InvocationCancel request)
        {
            _invocationHandler.TryCancel(request.InvocationId);
        }

        internal static WorkerInitResponse WorkerInitRequestHandler(WorkerInitRequest request, WorkerOptions workerOptions)
        {
            var frameworkDescriptionRegex = new Regex(@"^(\D*)+(?!\S)");

            var response = new WorkerInitResponse
            {
                Result = new StatusResult { Status = StatusResult.Types.Status.Success },
                WorkerVersion = WorkerInformation.Instance.WorkerVersion,
                WorkerMetadata = new WorkerMetadata
                {
                    RuntimeName = frameworkDescriptionRegex.Match(RuntimeInformation.FrameworkDescription).Value ?? RuntimeInformation.FrameworkDescription,
                    RuntimeVersion = Environment.Version.ToString(),
                    WorkerVersion = WorkerInformation.Instance.WorkerVersion,
                    WorkerBitness = RuntimeInformation.ProcessArchitecture.ToString()
                }
            };

            response.WorkerMetadata.CustomProperties.Add("Worker.Grpc.Version", typeof(GrpcWorker).Assembly.GetName().Version?.ToString());

            response.Capabilities.Add("RpcHttpBodyOnly", bool.TrueString);
            response.Capabilities.Add("RawHttpBodyBytes", bool.TrueString);
            response.Capabilities.Add("RpcHttpTriggerMetadataRemoved", bool.TrueString);
            response.Capabilities.Add("UseNullableValueDictionaryForHttp", bool.TrueString);
            response.Capabilities.Add("TypedDataCollection", bool.TrueString);
            response.Capabilities.Add("WorkerStatus", bool.TrueString);
            response.Capabilities.Add("HandlesWorkerTerminateMessage", bool.TrueString);
            response.Capabilities.Add("HandlesInvocationCancelMessage", bool.TrueString);

            if (workerOptions.EnableUserCodeException)
            {
                response.Capabilities.Add("EnableUserCodeException", bool.TrueString);
            }
            if (workerOptions.IncludeEmptyEntriesInMessagePayload)
            {
                response.Capabilities.Add("IncludeEmptyEntriesInMessagePayload", bool.TrueString);
            }

            return response;
        }

        private async Task<FunctionMetadataResponse> GetFunctionMetadataAsync(string functionAppDirectory)
        {
            var response = new FunctionMetadataResponse
            {
                Result = StatusResult.Success,
                UseDefaultMetadataIndexing = false
            };

            try
            {
                var functionMetadataList = await _functionMetadataProvider.GetFunctionMetadataAsync(functionAppDirectory);

                foreach (var func in functionMetadataList)
                {
                    if (func is null)
                    {
                        continue;
                    }

                    if (func.RawBindings?.Any() != true)
                    {
                        throw new InvalidOperationException($"Functions must declare at least one binding. No bindings were found in the function ${nameof(func)}.");
                    }

                    var rpcFuncMetadata = func switch
                    {
                        RpcFunctionMetadata rpc => rpc,
                        _ => BuildRpc(func),
                    };

                    // add BindingInfo here instead of in the providers  
                    // because we need access to gRPC types in proto-file and source-gen won't have access
                    rpcFuncMetadata.Bindings.Add(func.GetBindingInfoList());

                    response.FunctionMetadataResults.Add(rpcFuncMetadata);
                }

            }
            catch (Exception ex)
            {
                response.Result = new StatusResult
                {
                    Status = StatusResult.Types.Status.Failure,
                    Exception = ex.ToRpcException()
                };
            }

            return response;
        }

        private static RpcFunctionMetadata BuildRpc(IFunctionMetadata func)
        {
            // create RpcFunctionMetadata
            var rpcFuncMetadata = new RpcFunctionMetadata
            {
                Name = func.Name,
                EntryPoint = func.EntryPoint,
                FunctionId = func.FunctionId,
                IsProxy = false,
                Language = func.Language,
                ScriptFile = func.ScriptFile,
            };

            // Add raw bindings
            foreach (var rawBinding in func.RawBindings!)
            {
                rpcFuncMetadata.RawBindings.Add(rawBinding);
            }

            return rpcFuncMetadata;
        }

        internal void WorkerTerminateRequestHandler(WorkerTerminate request)
        {
            _hostApplicationLifetime.StopApplication();
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
