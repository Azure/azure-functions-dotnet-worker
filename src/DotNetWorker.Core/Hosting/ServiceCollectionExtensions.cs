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
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.Logging;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            services.TryAddSingleton<IFunctionsApplication, FunctionsApplication>();

            // Execution
            services.TryAddSingleton<IMethodInfoLocator, DefaultMethodInfoLocator>();
            services.TryAddSingleton<IFunctionInvokerFactory, DefaultFunctionInvokerFactory>();
            services.TryAddSingleton<IMethodInvokerFactory, DefaultMethodInvokerFactory>();
            services.TryAddSingleton<IFunctionActivator, DefaultFunctionActivator>();
            services.TryAddSingleton<IFunctionExecutor, DefaultFunctionExecutor>();

            // Function Execution Contexts
            services.TryAddSingleton<IFunctionContextFactory, DefaultFunctionContextFactory>();

            // Invocation Features
            services.TryAddSingleton<IInvocationFeaturesFactory, DefaultInvocationFeaturesFactory>();
            services.TryAddSingleton<IInvocationFeatureProvider, DefaultBindingFeatureProvider>();

            // Input conversion feature
            services.TryAddSingleton<IConverterContextFactory, DefaultConverterContextFactory>();
            services.TryAddSingleton<IInputConversionFeatureProvider, DefaultInputConversionFeatureProvider>();
            services.TryAddSingleton<IInputConverterProvider, DefaultInputConverterProvider>();

            // Input binding cache
            services.TryAddScoped<IBindingCache<ConversionResult>, DefaultBindingCache<ConversionResult>>();

            // Output Bindings
            services.TryAddSingleton<IOutputBindingsInfoProvider, DefaultOutputBindingsInfoProvider>();

            // Worker initialization service
            services.AddHostedService<WorkerHostedService>();

            // Worker metadata management
            services.TryAddSingleton<IFunctionMetadataManager, DefaultFunctionMetadataManager>();

            // Default serializer settings
            services.AddOptions();
            services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<WorkerOptions>, WorkerOptionsSetup>());

            services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, WorkerLoggerProvider>());

            services.TryAddSingleton(NullLogWriter.Instance);
            services.TryAddSingleton<IUserLogWriter>(s => s.GetRequiredService<NullLogWriter>());
            services.TryAddSingleton<ISystemLogWriter>(s => s.GetRequiredService<NullLogWriter>());
            services.TryAddSingleton<IUserMetricWriter>(s => s.GetRequiredService<NullLogWriter>());
            services.TryAddSingleton<FunctionActivitySourceFactory>();

            if (configure != null)
            {
                services.Configure(configure);
            }

            IFunctionsWorkerApplicationBuilder builder = null!;

            // We want to ensure that if this is called multiple times, we use the same builder,
            // so we stash in the IServiceCollection for future calls to check.
            foreach (var descriptor in services)
            {
                if (descriptor.ServiceType == typeof(IFunctionsWorkerApplicationBuilder))
                {
                    builder = (IFunctionsWorkerApplicationBuilder)descriptor.ImplementationInstance!;
                    break;
                }
            }

            if (builder is null)
            {
                builder = new FunctionsWorkerApplicationBuilder(services);
                services.AddSingleton<IFunctionsWorkerApplicationBuilder>(builder);

                // Execute startup code from worker extensions if present
                // Only run this once when builder is first added.
                RunExtensionStartupCode(builder);
            }

            return builder;
        }

        /// <summary>
        /// Adds the built-in input converters to worker options.
        /// </summary>
        internal static IServiceCollection AddDefaultInputConvertersToWorkerOptions(this IServiceCollection services)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<WorkerOptions>, DefaultInputConverterInitializer>());
            return services;
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

        private sealed class WorkerOptionsSetup(IOptions<JsonSerializerOptions> serializerOptions) : IPostConfigureOptions<WorkerOptions>
        {
            public void PostConfigure(string? name, WorkerOptions options)
            {
                options.Serializer ??= new JsonObjectSerializer(serializerOptions.Value);
            }
        }
    }
}
