// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Azure.Core.Serialization;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Provides extension methods to work with a <see cref="IFunctionsWorkerApplicationBuilder"/>.
    /// </summary>
    public static class FunctionsWorkerApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures the CosmosDB extension.
        /// </summary>
        /// <param name="builder">The <see cref="IFunctionsWorkerApplicationBuilder"/> to configure.</param>
        /// <returns>The same instance of the <see cref="IFunctionsWorkerApplicationBuilder"/> for chaining.</returns>
        public static IFunctionsWorkerApplicationBuilder ConfigureCosmosDBExtension(
            this IFunctionsWorkerApplicationBuilder builder)
            => ConfigureCosmosDBExtension(builder, configure: null);

        /// <summary>
        /// Configures the CosmosDB extension.
        /// </summary>
        /// <param name="builder">The <see cref="IFunctionsWorkerApplicationBuilder"/> to configure.</param>
        /// <param name="configure">An action to configure the CosmosDB extension options.</param>
        /// <returns>The same instance of the <see cref="IFunctionsWorkerApplicationBuilder"/> for chaining.</returns>
        public static IFunctionsWorkerApplicationBuilder ConfigureCosmosDBExtension(
            this IFunctionsWorkerApplicationBuilder builder, Action<OptionsBuilder<CosmosDBExtensionOptions>>? configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddAzureClientsCore(); // Adds AzureComponentFactory
            builder.Services.AddOptions<CosmosDBBindingOptions>();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<
                IPostConfigureOptions<CosmosDBExtensionOptions>, PostConfigureCosmosDBExtensionOptions>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<
                IConfigureOptions<CosmosDBBindingOptions>, CosmosDBBindingOptionsSetup>());

            if (configure is not null)
            {
                OptionsBuilder<CosmosDBExtensionOptions> options = builder.Services.AddOptions<CosmosDBExtensionOptions>();
                configure(options);
            }

            return builder;
        }

        /// <summary>
        /// Configures the CosmosDBExtensionOptions for the Functions Worker Cosmos extension.
        /// </summary>
        /// <param name="builder">The IFunctionsWorkerApplicationBuilder to add the configuration to.</param>
        /// <param name="options">An Action to configure the CosmosDBExtensionOptions.</param>
        /// <returns>The same instance of the <see cref="IFunctionsWorkerApplicationBuilder"/> for chaining.</returns>
        public static IFunctionsWorkerApplicationBuilder ConfigureCosmosDBExtensionOptions(
            this IFunctionsWorkerApplicationBuilder builder, Action<CosmosDBExtensionOptions> options)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.Configure(options);
            return builder;
        }

        /// <summary>
        /// Configures the CosmosDB extension to use the WorkerOptions.Serializer for deserializing POCOs.
        /// Call this method to ensure custom serialization settings from WorkerOptions are used for CosmosDB bindings.
        /// </summary>
        /// <param name="builder">The <see cref="IFunctionsWorkerApplicationBuilder"/> to configure.</param>
        /// <returns>The same instance of the <see cref="IFunctionsWorkerApplicationBuilder"/> for chaining.</returns>
        public static IFunctionsWorkerApplicationBuilder UseCosmosDBWorkerSerializer(this IFunctionsWorkerApplicationBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddOptions<CosmosDBExtensionOptions>().UseWorkerSerializer();
            return builder;
        }

        /// <summary>
        /// Configures the CosmosDB extension to use the WorkerOptions.Serializer for deserializing POCOs.
        /// Call this method to ensure custom serialization settings from WorkerOptions are used for CosmosDB bindings.
        /// </summary>
        /// <param name="builder">The <see cref="IFunctionsWorkerApplicationBuilder"/> to configure.</param>
        /// <returns>The same instance of the <see cref="IFunctionsWorkerApplicationBuilder"/> for chaining.</returns>
        public static OptionsBuilder<CosmosDBExtensionOptions> UseWorkerSerializer(this OptionsBuilder<CosmosDBExtensionOptions> builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Configure<IOptions<WorkerOptions>>((cosmos, worker) =>
            {
                cosmos.Serializer = worker.Value.Serializer;
            });

            return builder;
        }

         private class PostConfigureCosmosDBExtensionOptions(IOptions<WorkerOptions> workerOptions)
            : IPostConfigureOptions<CosmosDBExtensionOptions>
        {
            public void PostConfigure(string? name, CosmosDBExtensionOptions options)
            {
                ObjectSerializer? serializer = options.Serializer ?? workerOptions.Value.Serializer;
                if (serializer is not null && options.ClientOptions.Serializer is null)
                {
                    options.ClientOptions.Serializer = new WorkerCosmosSerializer(serializer);
                }
            }
        }
    }
}
