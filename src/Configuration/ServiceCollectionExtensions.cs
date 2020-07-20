using System;
using Grpc.Core;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using FunctionsDotNetWorker.Converters;
using System.Collections.Concurrent;
using FunctionsDotNetWorker.Logging;

namespace FunctionsDotNetWorker.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IDotNetApplicationBuilder AddDotNetWorker(this IServiceCollection services, Action<DotNetWorkerOptions> configure)
        {
            services.AddSingleton<IParameterConverter, HttpRequestDataConverter>();
            services.AddSingleton<IParameterConverter, JsonPocoConverter>();
            services.AddSingleton<WorkerLogManager>();
            services.AddSingleton<ParameterConverterManager>();
            services.AddSingleton<FunctionBroker>();
            services.AddSingleton<BlockingCollection<StreamingMessage>>();

            services.AddSingleton(p =>
            {
                IOptions<WorkerArguments> argumentsOptions = p.GetService<IOptions<WorkerArguments>>();
                WorkerArguments arguments = argumentsOptions.Value;

                Channel channel = new Channel(arguments.Host, arguments.Port, ChannelCredentials.Insecure);
                return new FunctionRpcClient(new FunctionRpc.FunctionRpcClient(channel), arguments.WorkerId, p.GetService<FunctionBroker>(), p.GetService<WorkerLogManager>());
            });

            services.AddSingleton<IHostedService, MyHostedService>();

            services.AddOptions<WorkerArguments>()
                .Configure<IConfiguration>((arguments, config) =>
                {
                    config.Bind(arguments);
                });

            return new DotNetApplicationBuilder(services);
        }
    }
}
