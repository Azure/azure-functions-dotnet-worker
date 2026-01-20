// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Handlers;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.Rpc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MsgType = Microsoft.Azure.Functions.Worker.Grpc.Messages.StreamingMessage.ContentOneofCase;

namespace Microsoft.Azure.Functions.Worker
{
    internal partial class GrpcWorker : IWorker, IMessageProcessor
    {
        private readonly IFunctionsApplication _application;
        private readonly IMethodInfoLocator _methodInfoLocator;
        private readonly WorkerOptions _workerOptions;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IWorkerClientFactory _workerClientFactory;
        private readonly IInvocationHandler _invocationHandler;
        private readonly IFunctionMetadataManager _metadataManager;
        private IWorkerClient? _workerClient;

        public GrpcWorker(IFunctionsApplication application,
                          IWorkerClientFactory workerClientFactory,
                          IMethodInfoLocator methodInfoLocator,
                          IOptions<WorkerOptions> workerOptions,
                          IFunctionMetadataManager metadataManager,
                          IHostApplicationLifetime hostApplicationLifetime,
                          IInvocationHandler invocationHandler)
        {
            _hostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
            _workerClientFactory = workerClientFactory ?? throw new ArgumentNullException(nameof(workerClientFactory));
            _application = application ?? throw new ArgumentNullException(nameof(application));
            _methodInfoLocator = methodInfoLocator ?? throw new ArgumentNullException(nameof(methodInfoLocator));
            _workerOptions = workerOptions.Value ?? throw new ArgumentNullException(nameof(workerOptions));
            _metadataManager = metadataManager ?? throw new ArgumentNullException(nameof(metadataManager));

            _invocationHandler = invocationHandler;
        }

        public Task StartAsync(CancellationToken token)
        {
            _workerClient = _workerClientFactory.CreateClient(this);
            return _workerClient.StartAsync(token);
        }

        public Task StopAsync(CancellationToken token) => Task.CompletedTask;

        Task IMessageProcessor.ProcessMessageAsync(StreamingMessage message)
        {
            // Dispatch and return.
            Task.Run(() => ProcessRequestCoreAsync(message));

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
                    responseMessage.FunctionEnvironmentReloadResponse = EnvironmentReloadRequestHandler(_workerOptions);
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
            return _invocationHandler.InvokeAsync(request);
        }

        internal void InvocationCancelRequestHandler(InvocationCancel request)
        {
            _invocationHandler.TryCancel(request.InvocationId);
        }

        internal static WorkerInitResponse WorkerInitRequestHandler(WorkerInitRequest request, WorkerOptions workerOptions)
        {
            var response = new WorkerInitResponse
            {
                Result = new StatusResult { Status = StatusResult.Types.Status.Success },
                WorkerVersion = WorkerInformation.Instance.WorkerVersion,
                WorkerMetadata = GetWorkerMetadata()
            };

            response.Capabilities.Add(GetWorkerCapabilities(workerOptions));

            var appCapabilities = new Dictionary<string, string>();
            appCapabilities.Add("key1", "value1"); // Placeholder for future app capabilities
            appCapabilities.Add("key2", "value2");
            response.AppCapabilities.Add(appCapabilities);

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
                var functionMetadataList = await _metadataManager.GetFunctionMetadataAsync(functionAppDirectory);

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

                    if (func.Retry != null)
                    {
                        rpcFuncMetadata.RetryOptions = func.Retry switch
                        {
                            RpcRetryOptions retry => retry,
                            _ => BuildRpcRetry(func.Retry)
                        };
                    }

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

        private static RpcRetryOptions BuildRpcRetry(IRetryOptions retry)
        {
            var rpcRetryOptions = new RpcRetryOptions
            {
                MaxRetryCount = retry.MaxRetryCount
            };

            if (retry.Strategy is RetryStrategy.FixedDelay)
            {
                rpcRetryOptions.RetryStrategy = RpcRetryOptions.Types.RetryStrategy.FixedDelay;
                rpcRetryOptions.DelayInterval = Duration.FromTimeSpan((TimeSpan) retry.DelayInterval!);
            }
            else if (retry.Strategy is RetryStrategy.ExponentialBackoff)
            {
                rpcRetryOptions.RetryStrategy = RpcRetryOptions.Types.RetryStrategy.ExponentialBackoff;
                rpcRetryOptions.MaximumInterval = Duration.FromTimeSpan((TimeSpan) retry.MaximumInterval!);
                rpcRetryOptions.MinimumInterval = Duration.FromTimeSpan((TimeSpan)retry.MinimumInterval!);
            }
            else
            {
                throw new InvalidOperationException($"Unknown retry strategy: ${nameof(retry.Strategy)}.");
            }

            return rpcRetryOptions;
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

        internal static FunctionEnvironmentReloadResponse EnvironmentReloadRequestHandler(WorkerOptions workerOptions)
        {
            var envReloadResponse = new FunctionEnvironmentReloadResponse
            {
                Result = new StatusResult { Status = StatusResult.Types.Status.Success },
                WorkerMetadata = GetWorkerMetadata()
            };
            envReloadResponse.Capabilities.Add(GetWorkerCapabilities(workerOptions));

            return envReloadResponse;
        }

#if NET7_0_OR_GREATER
        [GeneratedRegex(@"^(\D*)+(?!\S)")]
        private static partial Regex FrameworkDescriptionRegex();
#else 
        private static readonly Regex FrameworkDescriptionRegexBacking = new Regex(@"^(\D*)+(?!\S)");
        private static Regex FrameworkDescriptionRegex() => FrameworkDescriptionRegexBacking;
#endif


        internal static WorkerMetadata GetWorkerMetadata(string? frameworkDescription = null)
        {
            frameworkDescription ??= RuntimeInformation.FrameworkDescription;
            
            var match = FrameworkDescriptionRegex().Match(frameworkDescription);
            frameworkDescription = match.Success ? match.Value : frameworkDescription;

            var workerMetadata = new WorkerMetadata
            {
                RuntimeName = frameworkDescription,
                RuntimeVersion = Environment.Version.ToString(),
                WorkerVersion = WorkerInformation.Instance.WorkerVersion,
                WorkerBitness = RuntimeInformation.ProcessArchitecture.ToString()
            };
            workerMetadata.CustomProperties.Add("Worker.Grpc.Version", typeof(GrpcWorker).Assembly.GetName().Version?.ToString());

            return workerMetadata;
        }

        private static IDictionary<string, string> GetWorkerCapabilities(WorkerOptions workerOptions)
        {
            var capabilities = new Dictionary<string, string>();

            // Add additional capabilities defined by WorkerOptions
            foreach ((string key, string value) in workerOptions.Capabilities)
            {
                capabilities[key] = value;
            }

            // Add required capabilities; these cannot be modified and will override anything from WorkerOptions
            capabilities[WorkerCapabilities.RpcHttpBodyOnly] = bool.TrueString;
            capabilities[WorkerCapabilities.RawHttpBodyBytes] = bool.TrueString;
            capabilities[WorkerCapabilities.RpcHttpTriggerMetadataRemoved] = bool.TrueString;
            capabilities[WorkerCapabilities.UseNullableValueDictionaryForHttp] = bool.TrueString;
            capabilities[WorkerCapabilities.TypedDataCollection] = bool.TrueString;
            capabilities[WorkerCapabilities.WorkerStatus] = bool.TrueString;

            return capabilities;
        }
    }
}
