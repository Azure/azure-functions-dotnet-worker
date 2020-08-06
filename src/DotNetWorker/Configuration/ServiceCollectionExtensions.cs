using System;
using System.Threading.Channels;
using Grpc.Core;
using Microsoft.Azure.Functions.DotNetWorker.Converters;
using Microsoft.Azure.Functions.DotNetWorker.Descriptor;
using Microsoft.Azure.Functions.DotNetWorker.FunctionInvoker;
using Microsoft.Azure.Functions.DotNetWorker.Pipeline;
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
            services.AddSingleton<FunctionsHostOutputChannel>(s =>
            {
                UnboundedChannelOptions outputOptions = new UnboundedChannelOptions
                {
                    SingleWriter = false,
                    SingleReader = true,
                    AllowSynchronousContinuations = true
                };

                return new FunctionsHostOutputChannel(System.Threading.Channels.Channel.CreateUnbounded<StreamingMessage>(outputOptions));
            });

            // Request handling
            services.AddSingleton<IFunctionsHostClient, DefaultFunctionsHostClient>();
            services.AddSingleton<IHostRequestHandler, DefaultHostRequestHandler>();
            services.AddSingleton<IFunctionInstanceFactory, DefaultFunctionInstanceFactory>();
            services.AddSingleton<IFunctionBroker, FunctionBroker>();
            services.AddSingleton<IFunctionInvoker, DefaultFunctionInvoker>();

            // Function Execution Contexts
            services.AddSingleton<IFunctionExecutionContextFactory, DefaultFunctionExecutionFactory>();

            // Function Descriptor
            services.AddSingleton<IFunctionDescriptorFactory, DefaultFunctionDescriptorFactory>();

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
