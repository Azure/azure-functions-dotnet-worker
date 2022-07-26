// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.Logging;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Azure Functions extensions for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the core set of services for the Azure Functions worker.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">The action used to configure <see cref="WorkerOptions"/>.</param>
        /// <returns>The same <see cref="IFunctionsWorkerApplicationBuilder"/> for chaining.</returns>
        public static IFunctionsWorkerApplicationBuilder AddFunctionsWorkerCore(this IServiceCollection services, Action<WorkerOptions>? configure = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Request handling
            services.AddSingleton<IFunctionsApplication, FunctionsApplication>();

            // Execution
            services.AddSingleton<IMethodInfoLocator, DefaultMethodInfoLocator>();
            services.AddSingleton<IFunctionInvokerFactory, DefaultFunctionInvokerFactory>();
            services.AddSingleton<IMethodInvokerFactory, DefaultMethodInvokerFactory>();
            services.AddSingleton<IFunctionActivator, DefaultFunctionActivator>();
            services.AddSingleton<IFunctionExecutor, DefaultFunctionExecutor>();

            // Function Execution Contexts
            services.AddSingleton<IFunctionContextFactory, DefaultFunctionContextFactory>();

            // Invocation Features
            services.TryAddSingleton<IInvocationFeaturesFactory, DefaultInvocationFeaturesFactory>();
            services.AddSingleton<IInvocationFeatureProvider, DefaultBindingFeatureProvider>();

            // Input conversion feature
            services.AddSingleton<IConverterContextFactory, DefaultConverterContextFactory>();
            services.AddSingleton<IInputConversionFeatureProvider, DefaultInputConversionFeatureProvider>();
            services.AddSingleton<IInputConverterProvider, DefaultInputConverterProvider>();

            // Input binding cache
            services.AddScoped<IBindingCache<ConversionResult>, DefaultBindingCache<ConversionResult>>();

            // Output Bindings
            services.AddSingleton<IOutputBindingsInfoProvider, DefaultOutputBindingsInfoProvider>();

            // Worker initialization service
            services.AddSingleton<IHostedService, WorkerHostedService>();

            // Default serializer settings
            services.AddOptions<WorkerOptions>()
                .PostConfigure<IOptions<JsonSerializerOptions>>((workerOptions, serializerOptions) =>
                {
                    if (workerOptions.Serializer is null)
                    {
                        workerOptions.Serializer = new JsonObjectSerializer(serializerOptions.Value);
                    }
                });

            services.AddSingleton<ILoggerProvider, WorkerLoggerProvider>();
            services.AddSingleton(NullLogWriter.Instance);
            services.AddSingleton<IUserLogWriter>(s => s.GetRequiredService<NullLogWriter>());
            services.AddSingleton<ISystemLogWriter>(s => s.GetRequiredService<NullLogWriter>());
            services.AddSingleton<IUserMetricWriter>(s => s.GetRequiredService<NullLogWriter>());

            if (configure != null)
            {
                services.Configure(configure);
            }

            var builder = new FunctionsWorkerApplicationBuilder(services);
            // Execute startup code from worker extensions if present.
            RunExtensionStartupCode(builder);

            return builder;
        }

        /// <summary>
        /// Adds the built-in input converters to worker options.
        /// </summary>
        internal static IServiceCollection AddDefaultInputConvertersToWorkerOptions(this IServiceCollection services)
        {
            return services.Configure<WorkerOptions>((workerOption) =>
            {
                workerOption.InputConverters.Register<FunctionContextConverter>();
                workerOption.InputConverters.Register<TypeConverter>();
                workerOption.InputConverters.Register<GuidConverter>();
                workerOption.InputConverters.Register<DateTimeConverter>();
                workerOption.InputConverters.Register<MemoryConverter>();
                workerOption.InputConverters.Register<StringToByteConverter>();
                workerOption.InputConverters.Register<JsonPocoConverter>();
                workerOption.InputConverters.Register<ArrayConverter>();
                workerOption.InputConverters.Register<CancellationTokenConverter>();
            });
        }

        /// <summary>
        /// Run extension startup execution code.
        /// Our source generator creates a class(WorkerExtensionStartupCodeExecutor)
        /// which internally calls the "Configure" method on each of the participating
        /// extensions. Here we are calling the uber "Configure" method on the generated class.
        /// </summary>
        private static void RunExtensionStartupCode(IFunctionsWorkerApplicationBuilder builder)
        {
            var entryAssembly = Assembly.GetEntryAssembly()!;

            // Find the assembly attribute which has information about the startup code executor class
            var startupCodeExecutorInfoAttr = entryAssembly.GetCustomAttribute<WorkerExtensionStartupCodeExecutorInfoAttribute>();

            // Our source generator will not create the WorkerExtensionStartupCodeExecutor class
            // and will not add the above assembly attribute when no extension startup hooks are found.
            if (startupCodeExecutorInfoAttr == null)
            {
                return;
            }

            var startupCodeExecutorInstance =
                Activator.CreateInstance(startupCodeExecutorInfoAttr.StartupCodeExecutorType) as WorkerExtensionStartup;
            startupCodeExecutorInstance!.Configure(builder);
        }
    }
}
