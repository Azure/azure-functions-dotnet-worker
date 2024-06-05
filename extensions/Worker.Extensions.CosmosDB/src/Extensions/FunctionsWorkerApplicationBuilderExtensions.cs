// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Cosmos;
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
        public static IFunctionsWorkerApplicationBuilder ConfigureCosmosDBExtension(this IFunctionsWorkerApplicationBuilder builder)
        {
            if (builder is null)
            {
                throw new System.ArgumentNullException(nameof(builder));
            }

            builder.Services.AddAzureClientsCore(); // Adds AzureComponentFactory
            builder.Services.AddOptions<CosmosDBBindingOptions>();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<CosmosDBBindingOptions>, CosmosDBBindingOptionsSetup>());

            return builder;
        }

        /// <summary>
        /// Configures the CosmosDBOptions for the Functions Worker Cosmos extension.
        /// </summary>
        /// <param name="builder">The IFunctionsWorkerApplicationBuilder to add the configuration to.</param>
        /// <param name="options">An Action to configure the CosmosDBOptions.</param>
        /// <returns>The same instance of the <see cref="IFunctionsWorkerApplicationBuilder"/> for chaining.</returns>
        public static IFunctionsWorkerApplicationBuilder ConfigureCosmosDBExtensionOptions(this IFunctionsWorkerApplicationBuilder builder, Action<CosmosDBOptions> options)
        {
            builder.Services.Configure(options);
            return builder;
        }
    }
}
