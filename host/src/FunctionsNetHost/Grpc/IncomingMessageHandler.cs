// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Channels;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace FunctionsNetHost.Grpc
{

    internal class IncomingMessageHandler
    {
        private readonly Channel<StreamingMessage> _outgoingMessageChannel;
        private bool _specializationDone;
        AppLoader _appLoader;
        public IncomingMessageHandler(Channel<StreamingMessage> outgoingMessageChannel, AppLoader appLoader)
        {
            _outgoingMessageChannel = outgoingMessageChannel;
            _appLoader = appLoader;
        }

        internal Task ProcessMessageAsync(StreamingMessage message)
        {
            Task.Run(() => Process(message));

            return Task.CompletedTask;
        }

        private async Task Process(StreamingMessage msg)
        {
            Logger.Log($"New message received in client:{msg.ContentCase}");

            if (_specializationDone)
            {
                // Specialization done. So we will simply forward all messages to customer payload.
                Logger.Log($"Specialization done. Forwarding messages to customer payload:{msg.ContentCase}");
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
                        var metadataResponse = new FunctionMetadataResponse
                        {
                            UseDefaultMetadataIndexing = true,
                            Result = new StatusResult { Status = StatusResult.Types.Status.Success }
                        }
                        ;
                        responseMessage.FunctionMetadataResponse = metadataResponse;
                        break;
                    }
                case StreamingMessage.ContentOneofCase.FunctionEnvironmentReloadRequest:
                    {
                        IDictionary<string, string> environmentVariables = msg.FunctionEnvironmentReloadRequest.EnvironmentVariables;
                        foreach(var  kv in environmentVariables)
                        {
                            Environment.SetEnvironmentVariable(kv.Key, kv.Value);
                        }
                        Logger.Log($"Set {environmentVariables.Keys.Count()} EnvironmentVariables");

                        var functionAppDirectory = msg.FunctionEnvironmentReloadRequest.FunctionAppDirectory;

                        var applicationExePath = PathUtils.GetApplicationExePath(functionAppDirectory);
                        //Logger.Log($"applicationExePath: {applicationExePath}");

                        Logger.Log($"Before calling RunApplication");

                        Thread newThread = new Thread(() =>
                        {
                            //Logger.Log($"Before calling RunApplication Inside Thread");
                            _ = _appLoader.RunApplication(applicationExePath);
                            //Logger.Log($"After calling RunApplication inside Thread");
                        });
                        newThread.Start();

                        //Task.Run(() =>
                        //{
                        //    Logger.Log($"Before calling RunApplication Inside Task1");
                        //    _ = _appLoader.RunApplication(applicationExePath);
                        //    Logger.Log($"After calling RunApplication inside Task");
                        //});
                        
                        Logger.Log($"After calling RunApplication11. Will WaitOne until signlated");
                        
                        SignalManager.Instance.Signal.WaitOne();
                        Logger.Log($"signlated. Will forrward env reload req");

                        // TO DO:  wait until we get a signal that it is loaded.
                        _specializationDone = true;

                        // Forward the env reload request to customer payload.
                        await MessageChannel.Instance.SendInboundAsync(msg);
                        break;
                    }
            }

            await MessageChannel.Instance.SendOutboundAsync(responseMessage);
           // await _outgoingMessageChannel.Writer.WriteAsync(responseMessage);
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
