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
                Logger.Log("Starting FunctionsNetHost");

                var workerStartupOptions = await GetStartupOptionsFromCmdLineArgs(args);

                using var appLoader = new AppLoader();
                var grpcClient = new GrpcClient(workerStartupOptions, appLoader);

                await grpcClient.InitAsync();
            }
            catch (Exception exception)
            {
                Logger.Log($"An error occurred while running FunctionsNetHost.{exception}");
            }
        }

        private static async Task<GrpcWorkerStartupOptions> GetStartupOptionsFromCmdLineArgs(string[] args)
        {
            var hostOption = new Option<string>("--host");
            var portOption = new Option<int>("--port");
            var workerOption = new Option<string>("--workerId");
            var grpcMsgLengthOption = new Option<int>("--grpcMaxMessageLength");
            var requestIdOption = new Option<string>("--requestId");
            var funcUriOption = new Option<string>("--functions-uri");
            var funcRequestIdOption = new Option<string>("--functions-request-id");
            var funcWorkerIdOption = new Option<string>("--functions-worker-id");
            var funcGrpcMsgLengthOption = new Option<int?>("--functions-grpc-max-message-length");

            var rootCommand = new RootCommand
            {
                TreatUnmatchedTokensAsErrors = false
            };
            rootCommand.AddOption(portOption);
            rootCommand.AddOption(hostOption);
            rootCommand.AddOption(workerOption);
            rootCommand.AddOption(grpcMsgLengthOption);
            rootCommand.AddOption(requestIdOption);
            rootCommand.AddOption(funcUriOption);
            rootCommand.AddOption(funcRequestIdOption);
            rootCommand.AddOption(funcWorkerIdOption);
            rootCommand.AddOption(funcGrpcMsgLengthOption);

            var workerStartupOptions = new GrpcWorkerStartupOptions();

            rootCommand.SetHandler((context) =>
            {
                var serverEndpoint = context.ParseResult.GetValueForOption(funcUriOption) ?? $"http://{context.ParseResult.GetValueForOption(hostOption)}:{context.ParseResult.GetValueForOption(portOption)}";
                workerStartupOptions.ServerUri = new Uri(serverEndpoint);

                workerStartupOptions.GrpcMaxMessageLength = context.ParseResult.GetValueForOption(funcGrpcMsgLengthOption) ?? context.ParseResult.GetValueForOption(grpcMsgLengthOption);
                workerStartupOptions.RequestId = context.ParseResult.GetValueForOption(funcRequestIdOption) ?? context.ParseResult.GetValueForOption(requestIdOption);
                workerStartupOptions.WorkerId = context.ParseResult.GetValueForOption(funcWorkerIdOption) ?? context.ParseResult.GetValueForOption(workerOption);
            });

            Logger.LogTrace($"raw args:{string.Join(" ", args)}");

            var argsWithoutExecutableName = args.Skip(1).ToArray();
            await rootCommand.InvokeAsync(argsWithoutExecutableName);

            return workerStartupOptions;
        }
    }
}
