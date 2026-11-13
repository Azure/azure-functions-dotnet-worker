// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Extensions.AzureCosmosDb.Mongo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Provides extension methods to work with an <see cref="IFunctionsWorkerApplicationBuilder"/>.
    /// </summary>
    public static class CosmosDBMongoFunctionsWorkerApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures the Azure Cosmos DB for MongoDB (vCore) extension.
        /// </summary>
        /// <param name="builder">The <see cref="IFunctionsWorkerApplicationBuilder"/> to configure.</param>
        /// <returns>The same instance of the <see cref="IFunctionsWorkerApplicationBuilder"/> for chaining.</returns>
        public static IFunctionsWorkerApplicationBuilder ConfigureCosmosDBMongoExtension(
            this IFunctionsWorkerApplicationBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddOptions<MongoBindingOptions>();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<
                IConfigureOptions<MongoBindingOptions>, MongoBindingOptionsSetup>());

            return builder;
        }
    }
}