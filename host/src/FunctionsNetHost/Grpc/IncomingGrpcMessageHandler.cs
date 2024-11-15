﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using FunctionsNetHost.Prelaunch;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using InteropSpecializeMessage = FunctionsNetHost.Shared.Interop.SpecializeMessage;
namespace FunctionsNetHost.Grpc
{
    internal sealed class IncomingGrpcMessageHandler
    {
        private bool _specializationDone;
        private readonly AppLoader _appLoader;
        private readonly NetHostRunOptions _netHostRunOptions;

        internal IncomingGrpcMessageHandler(AppLoader appLoader, NetHostRunOptions netHostRunOptions)
        {
            _appLoader = appLoader;
            _netHostRunOptions = netHostRunOptions;
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
                        PreLauncher.Run();

                        responseMessage.WorkerWarmupResponse = new WorkerWarmupResponse
                        {
                            Result = new StatusResult { Status = StatusResult.Types.Status.Success }
                        };
                        break;
                    }
                case StreamingMessage.ContentOneofCase.FunctionEnvironmentReloadRequest:

                    Logger.Log("Specialization request received.");
                    var envReloadRequest = msg.FunctionEnvironmentReloadRequest;
                    foreach (var kv in envReloadRequest.EnvironmentVariables)
                    {
                        EnvironmentUtils.SetValue(kv.Key, kv.Value);
                    }
                    Configuration.Reload();

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

                    // On Unix-like systems, in-process environment modifications made by native libraries aren't seen by managed callers.
                    // So in the pre-jit case, we will send this env variables to managed code and set it there (for all OS).
                    // https://learn.microsoft.com/en-us/dotnet/api/system.environment.getenvironmentvariables
                    if (_netHostRunOptions.IsPreJitSupported)
                    {
                        var interopSpecializeMessage = new InteropSpecializeMessage
                        {
                            ApplicationExecutablePath = applicationExePath,
                            EnvironmentVariables = envReloadRequest.EnvironmentVariables
                        };
                        SignalStartupHook(interopSpecializeMessage);
                    }
                    else
                    {
#pragma warning disable CS4014
                        Task.Run(() => _appLoader.RunApplication(applicationExePath));
#pragma warning restore CS4014
                    }

                    Logger.LogTrace("Will wait for worker loaded signal.");
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

        private static void SignalStartupHook(InteropSpecializeMessage interopSpecializeMessage)
        {
            Logger.LogTrace("Sending specialization message to startuphook.");
            var messageByteArray = interopSpecializeMessage.ToByteArray();
            NativeHostApplication.Instance.HandleStartupHookInboundMessage(messageByteArray, messageByteArray.Length);
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
