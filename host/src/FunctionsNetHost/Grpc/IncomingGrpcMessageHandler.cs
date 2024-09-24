// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Pipes;
using FunctionsNetHost.Prelaunch;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

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

                    if (_netHostRunOptions.IsPreJitSupported)
                    {
                        // Signal so that startup hook load the payload assembly.
                        await NotifySpecializationOccured(applicationExePath);
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

        private static async Task NotifySpecializationOccured(string applicationExePath)
        {
            // Startup hook code has opened a named pipe server stream and waiting for a client to connect & send a message.
            try
            {
                using var pipeClient = new NamedPipeClientStream(".", Shared.Constants.NetHostWaitHandleName, PipeDirection.Out);
                await pipeClient.ConnectAsync();
                using var writer = new StreamWriter(pipeClient);
                writer.WriteLine(applicationExePath);
                writer.Flush();
                Logger.LogTrace("Sent application path to named pipe server stream.");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error connecting to named pipe server. {ex}");
                throw;
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
