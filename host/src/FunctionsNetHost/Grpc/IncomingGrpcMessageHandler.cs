// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
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
                        Logger.Log("Worker warmup request received. Pre-launcher skipped.");
                        // PreLauncher.Run();

                        responseMessage.WorkerWarmupResponse = new WorkerWarmupResponse
                        {
                            Result = new StatusResult { Status = StatusResult.Types.Status.Success }
                        };
                        break;
                    }
                case StreamingMessage.ContentOneofCase.FunctionEnvironmentReloadRequest:

                    var reloadStart = Stopwatch.GetTimestamp();
                    var stageStart = reloadStart;
                    var correlationId = Guid.NewGuid().ToString("N");
                    var envReloadRequest = msg.FunctionEnvironmentReloadRequest;

                    Logger.Log($"Function environment reload request received. CorrelationId:{correlationId}, FunctionAppDirectory:{envReloadRequest.FunctionAppDirectory}, EnvironmentVariableCount:{envReloadRequest.EnvironmentVariables.Count}");
                    Configuration.Reload();
                    LogEnvironmentReloadStage("configuration reload completed", correlationId, reloadStart, stageStart);

                    stageStart = Stopwatch.GetTimestamp();
                    var workerConfig = await WorkerConfigUtils.GetWorkerConfig(envReloadRequest.FunctionAppDirectory);
                    LogEnvironmentReloadStage("worker config read completed", correlationId, reloadStart, stageStart);

                    if (workerConfig?.Description is null)
                    {
                        LogEnvironmentReloadStage(
                            $"worker config not found in {envReloadRequest.FunctionAppDirectory}",
                            correlationId,
                            reloadStart,
                            reloadStart);
                        responseMessage.FunctionEnvironmentReloadResponse = BuildFailedEnvironmentReloadResponse();
                        break;
                    }

                    // function app payload which uses an older version of Microsoft.Azure.Functions.Worker package does not support specialization.
                    if (!workerConfig.Description.CanUsePlaceholder)
                    {
                        LogEnvironmentReloadStage(
                            "app payload does not support placeholder",
                            correlationId,
                            reloadStart,
                            reloadStart);
                        var e = new EnvironmentReloadNotSupportedException("This app is not using the latest version of Microsoft.Azure.Functions.Worker SDK and therefore does not leverage all performance optimizations. See https://aka.ms/azure-functions/dotnet/placeholders for more information.");
                        responseMessage.FunctionEnvironmentReloadResponse = BuildFailedEnvironmentReloadResponse(e);
                        break;
                    }

                    stageStart = Stopwatch.GetTimestamp();
                    var applicationExePath = Path.Combine(envReloadRequest.FunctionAppDirectory, workerConfig.Description.DefaultWorkerPath!);
                    LogEnvironmentReloadStage($"application path resolved: {applicationExePath}", correlationId, reloadStart, stageStart);

                    stageStart = Stopwatch.GetTimestamp();
                    foreach (var kv in envReloadRequest.EnvironmentVariables)
                    {
                        EnvironmentUtils.SetValue(kv.Key, kv.Value);
                    }
                    LogEnvironmentReloadStage(
                        $"environment variables applied. Count:{envReloadRequest.EnvironmentVariables.Count}",
                        correlationId,
                        reloadStart,
                        stageStart);

                    stageStart = Stopwatch.GetTimestamp();
                    var applicationLoadQueuedAt = stageStart;
#pragma warning disable CS4014
                    Task.Run(() =>
#pragma warning restore CS4014
                    {
                        var applicationLoadStart = Stopwatch.GetTimestamp();
                        Logger.Log($"Function environment reload: application load task started. CorrelationId:{correlationId}, QueueDelayMs:{Stopwatch.GetElapsedTime(applicationLoadQueuedAt, applicationLoadStart).TotalMilliseconds:0.0}, TotalElapsedMs:{Stopwatch.GetElapsedTime(reloadStart, applicationLoadStart).TotalMilliseconds:0.0}");
                        var exitCode = _appLoader.RunApplication(applicationExePath, correlationId);
                        LogEnvironmentReloadStage($"application load task completed. ExitCode:{exitCode}", correlationId, reloadStart, applicationLoadStart);
                    });
                    LogEnvironmentReloadStage("application load task queued", correlationId, reloadStart, stageStart);

                    stageStart = Stopwatch.GetTimestamp();
                    Logger.Log($"Function environment reload waiting for worker loaded signal. CorrelationId:{correlationId}");
                    WorkerLoadStatusSignalManager.Instance.Signal.WaitOne();
                    LogEnvironmentReloadStage("worker loaded signal received", correlationId, reloadStart, stageStart);

                    var logMessage = $"FunctionApp assembly loaded successfully. CorrelationId:{correlationId}, ProcessId:{Environment.ProcessId}";
                    if (OperatingSystem.IsWindows())
                    {
                        logMessage += $", AppPoolId:{Environment.GetEnvironmentVariable(EnvironmentVariables.AppPoolId)}";
                    }
                    Logger.Log(logMessage);

                    stageStart = Stopwatch.GetTimestamp();
                    Logger.Log($"Function environment reload: SendInboundAsync starting. CorrelationId:{correlationId}, TotalElapsedMs:{Stopwatch.GetElapsedTime(reloadStart, stageStart).TotalMilliseconds:0.0}");
                    await MessageChannel.Instance.SendInboundAsync(msg);
                    LogEnvironmentReloadStage("SendInboundAsync completed. Reload request forwarded to worker payload", correlationId, reloadStart, stageStart);
                    _specializationDone = true;
                    stageStart = Stopwatch.GetTimestamp();
                    LogEnvironmentReloadStage("specialization handoff completed", correlationId, reloadStart, stageStart);
                    break;
            }

            if (responseMessage.ContentCase != StreamingMessage.ContentOneofCase.None)
            {
                await MessageChannel.Instance.SendOutboundAsync(responseMessage);
            }
        }

        private static void LogEnvironmentReloadStage(string stage, string correlationId, long reloadStart, long stageStart)
        {
            var now = Stopwatch.GetTimestamp();
            Logger.Log(
                $"Function environment reload: {stage}. CorrelationId:{correlationId}, StepElapsedMs:{Stopwatch.GetElapsedTime(stageStart, now).TotalMilliseconds:0.0}, TotalElapsedMs:{Stopwatch.GetElapsedTime(reloadStart, now).TotalMilliseconds:0.0}");
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
