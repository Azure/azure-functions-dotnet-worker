using System;
using Grpc.Core;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using FunctionsDotNetWorker.Converters;
using System.Threading.Channels;
using FunctionsDotNetWorker.Logging;
using System.ComponentModel.DataAnnotations;

namespace FunctionsDotNetWorker.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IDotNetApplicationBuilder AddDotNetWorker(this IServiceCollection services, Action<DotNetWorkerOptions> configure)
        {
            services.AddSingleton<IParameterConverter, HttpRequestDataConverter>();
            services.AddSingleton<IParameterConverter, JsonPocoConverter>();
            services.AddSingleton<ParameterConverterManager>();
            services.AddSingleton<FunctionBroker>();
            services.AddSingleton(ch => 
            {
                return System.Threading.Channels.Channel.CreateUnbounded<StreamingMessage>();
            });

            services.AddSingleton(p =>
            {
                IOptions<WorkerArguments> argumentsOptions = p.GetService<IOptions<WorkerArguments>>();
                WorkerArguments arguments = argumentsOptions.Value;

                Grpc.Core.Channel grpcChannel = new Grpc.Core.Channel(arguments.Host, arguments.Port, ChannelCredentials.Insecure);
         
                return new FunctionRpcClient(new FunctionRpc.FunctionRpcClient(grpcChannel), arguments.WorkerId, p.GetService<FunctionBroker>(), p.GetService<Channel<StreamingMessage>>());
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
