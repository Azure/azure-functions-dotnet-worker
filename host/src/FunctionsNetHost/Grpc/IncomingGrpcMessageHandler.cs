// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using FunctionsNetHost.Prelaunch;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace FunctionsNetHost.Grpc
{
    internal sealed class IncomingGrpcMessageHandler
    {
        private bool _specializationDone;
        private readonly AppLoader _appLoader;
        private readonly GrpcWorkerStartupOptions _grpcWorkerStartupOptions;

        internal IncomingGrpcMessageHandler(AppLoader appLoader, GrpcWorkerStartupOptions grpcWorkerStartupOptions)
        {
            _appLoader = appLoader;
            _grpcWorkerStartupOptions = grpcWorkerStartupOptions;
        }

        internal Task ProcessMessageAsync(StreamingMessage message)
        {
            Task.Run(() => Process(message));

            return Task.CompletedTask;
        }

        private async Task Process(StreamingMessage msg)
        {
            if (_specializationDone)
            {
                // Specialization done. So forward all messages to customer payload.
                await MessageChannel.Instance.SendInboundAsync(msg);
                return;
            }

            var responseMessage = new StreamingMessage();

            switch (msg.ContentCase)
            {
                case StreamingMessage.ContentOneofCase.WorkerInitRequest:
                    {
                        responseMessage.WorkerInitResponse = BuildWorkerInitResponse();
                        break;
                    }
                case StreamingMessage.ContentOneofCase.FunctionsMetadataRequest:
                    {
                        responseMessage.FunctionMetadataResponse = BuildFunctionMetadataResponse();
                        break;
                    }
                case StreamingMessage.ContentOneofCase.WorkerWarmupRequest:
                    {
                        Logger.LogTrace("Worker warmup request received.");

                        // Asynchronously run the pre-launcher to warmup .net runtime.
                        _ = Task.Run(PreLauncher.Run);

                        responseMessage.WorkerWarmupResponse = new WorkerWarmupResponse
                        {
                            Result = new StatusResult { Status = StatusResult.Types.Status.Success }
                        };
                        break;
                    }
                case StreamingMessage.ContentOneofCase.FunctionEnvironmentReloadRequest:

                    Configuration.Reload();
                    Logger.LogTrace("Specialization request received.");

                    var envReloadRequest = msg.FunctionEnvironmentReloadRequest;

                    var workerConfig = await WorkerConfigUtils.GetWorkerConfig(envReloadRequest.FunctionAppDirectory);

                    if (workerConfig?.Description is null)
                    {
                        Logger.LogTrace($"Could not find a worker config in {envReloadRequest.FunctionAppDirectory}");
                        responseMessage.FunctionEnvironmentReloadResponse = BuildFailedEnvironmentReloadResponse();
                        break;
                    }

                    // function app payload which uses an older version of Microsoft.Azure.Functions.Worker package does not support specialization.
                    if (!workerConfig.Description.CanUsePlaceholder)
                    {
                        Logger.LogTrace("App payload uses an older version of Microsoft.Azure.Functions.Worker SDK which does not support placeholder.");
                        var e = new EnvironmentReloadNotSupportedException("This app is not using the latest version of Microsoft.Azure.Functions.Worker SDK and therefore does not leverage all performance optimizations. See https://aka.ms/azure-functions/dotnet/placeholders for more information.");
                        responseMessage.FunctionEnvironmentReloadResponse = BuildFailedEnvironmentReloadResponse(e);
                        break;
                    }

                    var applicationExePath = Path.Combine(envReloadRequest.FunctionAppDirectory, workerConfig.Description.DefaultWorkerPath!);
                    Logger.LogTrace($"application path {applicationExePath}");

                    foreach (var kv in envReloadRequest.EnvironmentVariables)
                    {
                        EnvironmentUtils.SetValue(kv.Key, kv.Value);
                    }

#pragma warning disable CS4014
                    Task.Run(() =>
#pragma warning restore CS4014
                    {
                        _ = _appLoader.RunApplication(applicationExePath);
                    });

                    Logger.LogTrace($"Will wait for worker loaded signal.");
                    WorkerLoadStatusSignalManager.Instance.Signal.WaitOne();

                    var logMessage = $"FunctionApp assembly loaded successfully. ProcessId:{Environment.ProcessId}";
                    if (OperatingSystem.IsWindows())
                    {
                        logMessage += $", AppPoolId:{Environment.GetEnvironmentVariable(EnvironmentVariables.AppPoolId)}";
                    }
                    Logger.Log(logMessage);

                    await MessageChannel.Instance.SendInboundAsync(msg);
                    _specializationDone = true;
                    break;
            }

            if (responseMessage.ContentCase != StreamingMessage.ContentOneofCase.None)
            {
                await MessageChannel.Instance.SendOutboundAsync(responseMessage);
            }
        }

        private static FunctionEnvironmentReloadResponse BuildFailedEnvironmentReloadResponse(Exception? exception = null)
        {
            var response = new FunctionEnvironmentReloadResponse
            {
                Result = new StatusResult
                {
                    Status = StatusResult.Types.Status.Failure
                }
            };

            response.Result.Exception = ToUserRpcException(exception);

            return response;
        }

        internal static RpcException? ToUserRpcException(Exception? exception)
        {
            if (exception is null)
            {
                return null;
            }

            return new RpcException
            {
                Message = exception.Message,
                Source = exception.Source ?? string.Empty,
                StackTrace = exception.StackTrace ?? string.Empty,
                Type = exception.GetType().FullName ?? string.Empty,
                IsUserException = true
            };
        }
        private static FunctionMetadataResponse BuildFunctionMetadataResponse()
        {
            var metadataResponse = new FunctionMetadataResponse
            {
                UseDefaultMetadataIndexing = true,
                Result = new StatusResult { Status = StatusResult.Types.Status.Success }
            };

            return metadataResponse;
        }

        private static WorkerInitResponse BuildWorkerInitResponse()
        {
            var response = new WorkerInitResponse
            {
                Result = new StatusResult { Status = StatusResult.Types.Status.Success }
            };
            response.Capabilities[WorkerCapabilities.EnableUserCodeException] = bool.TrueString;
            response.Capabilities[WorkerCapabilities.HandlesWorkerWarmupMessage] = bool.TrueString;

            return response;
        }
    }
}
