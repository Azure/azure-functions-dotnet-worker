// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Handlers;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.Rpc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MsgType = Microsoft.Azure.Functions.Worker.Grpc.Messages.StreamingMessage.ContentOneofCase;

namespace Microsoft.Azure.Functions.Worker
{
    internal class GrpcWorker : IWorker, IMessageProcessor
    {
        private readonly IFunctionsApplication _application;
        private readonly IMethodInfoLocator _methodInfoLocator;
        private readonly WorkerOptions _workerOptions;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IWorkerClientFactory _workerClientFactory;
        private readonly IInvocationHandler _invocationHandler;
        private readonly IFunctionMetadataProvider _functionMetadataProvider;
        private IWorkerClient? _workerClient;

        public GrpcWorker(IFunctionsApplication application,
                          IWorkerClientFactory workerClientFactory,
                          IMethodInfoLocator methodInfoLocator,
                          IOptions<WorkerOptions> workerOptions,
                          IFunctionMetadataProvider functionMetadataProvider,
                          IHostApplicationLifetime hostApplicationLifetime,
                          IInvocationHandler invocationHandler,
                          ILogger<GrpcWorker> logger)
        {
            _hostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
            _workerClientFactory = workerClientFactory ?? throw new ArgumentNullException(nameof(workerClientFactory));
            _application = application ?? throw new ArgumentNullException(nameof(application));
            _methodInfoLocator = methodInfoLocator ?? throw new ArgumentNullException(nameof(methodInfoLocator));

            _workerOptions = workerOptions?.Value ?? throw new ArgumentNullException(nameof(workerOptions));
            _functionMetadataProvider = functionMetadataProvider ?? throw new ArgumentNullException(nameof(functionMetadataProvider));
            _invocationHandler = invocationHandler;
        }

        public async Task StartAsync(CancellationToken token)
        {
            _workerClient = await _workerClientFactory.StartClientAsync(this, token);
        }

        public Task StopAsync(CancellationToken token) => Task.CompletedTask;

        Task IMessageProcessor.ProcessMessageAsync(StreamingMessage message)
        {
            // Dispatch and return.
            _ = ProcessRequestCoreAsync(message);

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

            await _workerClient!.SendMessageAsync(responseMessage);
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

            // Add additional capabilities defined by WorkerOptions
            foreach ((string key, string value) in workerOptions.Capabilities)
            {
                response.Capabilities[key] = value;
            }

            // Add required capabilities; these cannot be modified and will override anything from WorkerOptions
            response.Capabilities["RpcHttpBodyOnly"] = bool.TrueString;
            response.Capabilities["RawHttpBodyBytes"] = bool.TrueString;
            response.Capabilities["RpcHttpTriggerMetadataRemoved"] = bool.TrueString;
            response.Capabilities["UseNullableValueDictionaryForHttp"] = bool.TrueString;
            response.Capabilities["TypedDataCollection"] = bool.TrueString;
            response.Capabilities["WorkerStatus"] = bool.TrueString;

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
