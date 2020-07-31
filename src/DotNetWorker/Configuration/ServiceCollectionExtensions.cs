using System;
using System.Threading.Channels;
using Grpc.Core;
using Microsoft.Azure.Functions.DotNetWorker.Converters;
using Microsoft.Azure.Functions.DotNetWorker.FunctionInvoker;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.WebJobs.Script.Grpc.Messages.FunctionRpc;

namespace Microsoft.Azure.Functions.DotNetWorker.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IDotNetApplicationBuilder AddDotNetWorker(this IServiceCollection services, Action<DotNetWorkerOptions> configure)
        {
            // ParameterConverters
            services.AddSingleton<IParameterConverter, HttpRequestDataConverter>();
            services.AddSingleton<IParameterConverter, JsonPocoConverter>();
            services.AddSingleton<ParameterConverterManager>();

            // Channels
            services.AddSingleton<FunctionsHostChannelManager>(s =>
            {
                BoundedChannelOptions inputOptions = new BoundedChannelOptions(1000)
                {
                    SingleWriter = true,
                    SingleReader = false,
                    AllowSynchronousContinuations = true,
                    FullMode = BoundedChannelFullMode.Wait
                };
                var inputChannel = System.Threading.Channels.Channel.CreateBounded<StreamingMessage>(inputOptions);

                BoundedChannelOptions outputOptions = new BoundedChannelOptions(1000)
                {
                    SingleWriter = false,
                    SingleReader = true,
                    AllowSynchronousContinuations = true,
                    FullMode = BoundedChannelFullMode.Wait
                };
                var outputChannel = System.Threading.Channels.Channel.CreateBounded<StreamingMessage>(outputOptions);

                return new FunctionsHostChannelManager(inputChannel, outputChannel);
            });
            services.AddSingleton<FunctionsHostChannelWriter>();

            // Request handling
            services.AddSingleton<IHostRequestHandler, DefaultHostRequestHandler>();
            services.AddSingleton<IFunctionsHostClient, RxFunctionsHostClient>();
            services.AddSingleton<IFunctionInstanceFactory, DefaultFunctionInstanceFactory>();
            services.AddSingleton<IFunctionBroker, FunctionBroker>();
            services.AddSingleton<IFunctionInvoker, DefaultFunctionInvoker>();

            // gRpc
            services.AddSingleton<FunctionRpcClient>(p =>
            {
                IOptions<WorkerStartupOptions> argumentsOptions = p.GetService<IOptions<WorkerStartupOptions>>();
                WorkerStartupOptions arguments = argumentsOptions.Value;
                Grpc.Core.Channel grpcChannel = new Grpc.Core.Channel(arguments.Host, arguments.Port, ChannelCredentials.Insecure);

                return new FunctionRpcClient(grpcChannel);
            });
            services.AddSingleton<IHostedService, WorkerHostedService>();

            // Options
            services.AddOptions<WorkerStartupOptions>()
                .Configure<IConfiguration>((arguments, config) =>
                {
                    config.Bind(arguments);
                });

            return new DotNetApplicationBuilder(services);
        }
    }
}
