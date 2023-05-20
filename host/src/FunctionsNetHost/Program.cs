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
            Logger.Log("Starting FunctionsNetHost 5181024");

            GrpcWorkerStartupOptions workerStartupOptions = new();

            await ParseCommandLineArgs(args, workerStartupOptions);

            using (var appLoader = AppLoader.Instance)
            {
                var client = new GrpcClient(workerStartupOptions, appLoader);

                await client.InitAsync();
            }
        }

        private static async Task ParseCommandLineArgs(string[] args, GrpcWorkerStartupOptions grpcOptions)
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

            rootCommand.SetHandler((host, port, workerId, grpcMsgLength, requestId) =>
            {
                grpcOptions.Host = host;
                grpcOptions.Port = port;
                grpcOptions.WorkerId = workerId;
                grpcOptions.GrpcMaxMessageLength = grpcMsgLength;
                grpcOptions.RequestId = requestId;
            },
            hostOption, portOption, workerOption, grpcMsgLengthOption, requestIdOption);

            // If the first arg(exe name) has a .exe suffix, parsing fails. So exclude that.
            var cmdArgsString = string.Join(" ", args, startIndex: 1, count: args.Length - 1);
            await rootCommand.InvokeAsync(cmdArgsString);
        }
    }
}
