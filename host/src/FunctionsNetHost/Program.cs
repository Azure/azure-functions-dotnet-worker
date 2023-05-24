// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.CommandLine;
using FunctionsNetHost.Grpc;

namespace FunctionsNetHost
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Logger.LogInfo("Starting FunctionsNetHost");

                var workerStartupOptions = await GetStartupOptionsFromCmdLineArgs(args);

                using var appLoader = AppLoader.Instance;
                var grpcClient = new GrpcClient(workerStartupOptions, appLoader);

                await grpcClient.InitAsync();
            }
            catch (Exception exception)
            {
                Logger.LogInfo($"An error occurred while running FunctionsNetHost.{exception}");
            }
        }

        private static async Task<GrpcWorkerStartupOptions> GetStartupOptionsFromCmdLineArgs(string[] args)
        {
            var hostOption = new Option<string>("--host");
            var portOption = new Option<int>("--port");
            var workerOption = new Option<string>("--workerId");
            var grpcMsgLengthOption = new Option<int>("--grpcMaxMessageLength");
            var requestIdOption = new Option<string>("--requestId");

            var rootCommand = new RootCommand();
            rootCommand.AddOption(portOption);
            rootCommand.AddOption(hostOption);
            rootCommand.AddOption(workerOption);
            rootCommand.AddOption(grpcMsgLengthOption);
            rootCommand.AddOption(requestIdOption);

            var workerStartupOptions = new GrpcWorkerStartupOptions();

            rootCommand.SetHandler((host, port, workerId, grpcMsgLength, requestId) =>
                {
                    workerStartupOptions.Host = host;
                    workerStartupOptions.Port = port;
                    workerStartupOptions.WorkerId = workerId;
                    workerStartupOptions.GrpcMaxMessageLength = grpcMsgLength;
                    workerStartupOptions.RequestId = requestId;
                },
                hostOption, portOption, workerOption, grpcMsgLengthOption, requestIdOption);

            var argsWithoutExecutableName = args.Skip(1).ToArray();
            await rootCommand.InvokeAsync(argsWithoutExecutableName);

            return workerStartupOptions;
        }
    }
}
