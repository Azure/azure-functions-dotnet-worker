// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace FunctionsNetHost.Grpc
{
    internal sealed class IncomingGrpcMessageHandler
    {
        private bool _specializationDone;
        private readonly AppLoader _appLoader;

        internal IncomingGrpcMessageHandler(AppLoader appLoader)
        {
            _appLoader = appLoader;
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
                case StreamingMessage.ContentOneofCase.FunctionEnvironmentReloadRequest:

                    Logger.LogTrace("Specialization request received.");

                    var envReloadRequest = msg.FunctionEnvironmentReloadRequest;

                    var workerConfig = WorkerConfigUtils.GetWorkerConfig(envReloadRequest.FunctionAppDirectory);

                    if (workerConfig?.Description is null)
                    {
                        responseMessage.FunctionEnvironmentReloadResponse = BuildFailedEnvironmentReloadResponse(new FunctionAppPayloadNotFoundException());
                        break;
                    }

                    // function app payload which uses an older version of Microsoft.Azure.Functions.Worker package does not support specialization.
                    if (!workerConfig.Description.IsSpecializable)
                    {
                        Logger.LogTrace("App payload uses an older version of worker package which does not support specialization.");
                        responseMessage.FunctionEnvironmentReloadResponse = BuildFailedEnvironmentReloadResponse(new EnvironmentReloadUnsupportedException());
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
                    Logger.LogTrace($"Received worker loaded signal. Forwarding environment reload request to worker.");

                    await MessageChannel.Instance.SendInboundAsync(msg);
                    _specializationDone = true;
                    break;
            }

            if (responseMessage.ContentCase != StreamingMessage.ContentOneofCase.None)
            {
                await MessageChannel.Instance.SendOutboundAsync(responseMessage);
            }
        }

        private static FunctionEnvironmentReloadResponse BuildFailedEnvironmentReloadResponse(Exception exception)
        {
            var rpcException = new RpcException
            {
                Message = exception.ToString(),
                Source = exception.Source ?? string.Empty,
                StackTrace = exception.StackTrace ?? string.Empty
            };

            var response = new FunctionEnvironmentReloadResponse
            {
                Result = new StatusResult
                {
                    Status = StatusResult.Types.Status.Failure,
                    Exception = rpcException
                }
            };

            return response;
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

            return response;
        }
    }
}
