// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using FunctionsNetHost.Diagnostics;
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
                // For our tests, we will issue only one invocation request(cold start request)
                if (msg.ContentCase == StreamingMessage.ContentOneofCase.InvocationRequest)
                {
                    AppLoaderEventSource.Log.ColdStartRequestFunctionInvocationStart();
                }

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
                        AssemblyPreloader.Preload();

                        responseMessage.WorkerWarmupResponse = new WorkerWarmupResponse
                        {
                            Result = new StatusResult { Status = StatusResult.Types.Status.Success }
                        };
                        break;
                    }
                case StreamingMessage.ContentOneofCase.FunctionEnvironmentReloadRequest:

                    AppLoaderEventSource.Log.SpecializationRequestReceived();
                    Logger.LogTrace("Specialization request received.");

                    var envReloadRequest = msg.FunctionEnvironmentReloadRequest;

                    foreach (var kv in envReloadRequest.EnvironmentVariables)
                    {
                        EnvironmentUtils.SetValue(kv.Key, kv.Value);
                    }
                    //  signal the wait handle so that startup hook can continue executing
                    SpecializationSyncManager.WaitHandle.Set();

                    Logger.LogTrace($"Will wait for worker loaded signal.");
                    WorkerLoadStatusSignalManager.Instance.Signal.WaitOne();

                    AppLoaderEventSource.Log.ApplicationMainStartedSignalReceived();
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
