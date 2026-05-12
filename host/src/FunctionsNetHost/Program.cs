// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.CommandLine;
using System.Diagnostics;
using FunctionsNetHost.Grpc;

namespace FunctionsNetHost
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Logger.Log($"FunctionsNetHost process starting. TotalElapsedMs:{Logger.GetElapsedSinceProcessStart().TotalMilliseconds:0.0}");
                HostTraceManager.ConfigureAndStartDelayedFlush();

                // PreLauncher.Run();
                Logger.Log($"FunctionsNetHost pre-launcher skipped. TotalElapsedMs:{Logger.GetElapsedSinceProcessStart().TotalMilliseconds:0.0}");

                var stageStart = Stopwatch.GetTimestamp();
                Logger.Log($"Command-line parse starting. TotalElapsedMs:{Logger.GetElapsedSinceProcessStart().TotalMilliseconds:0.0}");
                var workerStartupOptions = await GetStartupOptionsFromCmdLineArgs(args);
                Logger.Log($"Command-line parse completed. StepElapsedMs:{Stopwatch.GetElapsedTime(stageStart).TotalMilliseconds:0.0}, TotalElapsedMs:{Logger.GetElapsedSinceProcessStart().TotalMilliseconds:0.0}");

                using var appLoader = new AppLoader(workerStartupOptions);
                var grpcClient = new GrpcClient(workerStartupOptions, appLoader);

                Logger.Log($"GrpcClient.InitAsync starting. TotalElapsedMs:{Logger.GetElapsedSinceProcessStart().TotalMilliseconds:0.0}");
                await grpcClient.InitAsync();
            }
            catch (Exception exception)
            {
                Logger.Log($"An error occurred while running FunctionsNetHost.{exception}");
            }
        }

        private static async Task<GrpcWorkerStartupOptions> GetStartupOptionsFromCmdLineArgs(string[] args)
        {
            var uriOption = new Option<string>("--functions-uri") { IsRequired = true };
            var requestIdOption = new Option<string>("--functions-request-id") { IsRequired = true };
            var workerIdOption = new Option<string>("--functions-worker-id") { IsRequired = true };
            var grpcMaxMessageLengthOption = new Option<int>("--functions-grpc-max-message-length") { IsRequired = true };

            var rootCommand = new RootCommand
            {
                TreatUnmatchedTokensAsErrors = false
            };

            rootCommand.AddOption(uriOption);
            rootCommand.AddOption(requestIdOption);
            rootCommand.AddOption(workerIdOption);
            rootCommand.AddOption(grpcMaxMessageLengthOption);

            var workerStartupOptions = new GrpcWorkerStartupOptions();

            rootCommand.SetHandler((context) =>
            {
                var uriString = context.ParseResult.GetValueForOption(uriOption);
                if (!Uri.TryCreate(uriString, UriKind.Absolute, out var endpointUri))
                {
                    throw new UriFormatException($"'{uriString}' is not a valid value for argument '{uriOption.Name}'. Value should be a valid URL.");
                }

                workerStartupOptions.ServerUri = endpointUri;
                workerStartupOptions.GrpcMaxMessageLength = context.ParseResult.GetValueForOption(grpcMaxMessageLengthOption);
                workerStartupOptions.RequestId = context.ParseResult.GetValueForOption(requestIdOption);
                workerStartupOptions.WorkerId = context.ParseResult.GetValueForOption(workerIdOption);
            });

            Logger.LogTrace($"raw args:{string.Join(" ", args)}");

            var argsWithoutExecutableName = args.Skip(1).ToArray();
            await rootCommand.InvokeAsync(argsWithoutExecutableName);

            workerStartupOptions.CommandLineArgs = argsWithoutExecutableName;

            return workerStartupOptions;
        }
    }
}
