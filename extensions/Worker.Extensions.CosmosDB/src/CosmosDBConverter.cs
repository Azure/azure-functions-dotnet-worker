// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Core;
using Azure.Cosmos;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// Converter to bind Blob Storage type parameters.
    /// </summary>
    internal class CosmosDBConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (context.Source is not IBindingData bindingData)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
            }

            bindingData.Properties.TryGetValue("connection_name", out var connectionName);
            bindingData.Properties.TryGetValue("database_name", out var databaseName);
            bindingData.Properties.TryGetValue("container_name", out var containerName);

            if (string.IsNullOrEmpty(connectionName)
                || string.IsNullOrEmpty(databaseName)
                || string.IsNullOrEmpty(containerName))
            {
                return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
            }

            var connectionString = Environment.GetEnvironmentVariable(connectionName);
            object result = ToTargetType(context.TargetType, connectionString, databaseName, containerName);

            if (result is not null)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Success(result));
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }

        private object? ToTargetType(Type targetType, string connectionString, string databaseName, string containerName) => targetType switch
        {
            Type _ when targetType == typeof(CosmosClient)      => CreateCosmosReference<CosmosClient>(connectionString, databaseName, containerName),
            Type _ when targetType == typeof(CosmosDatabase)    => CreateCosmosReference<CosmosDatabase>(connectionString, databaseName, containerName),
            Type _ when targetType == typeof(CosmosContainer)   => CreateCosmosReference<CosmosContainer>(connectionString, databaseName, containerName),
            _ => null
        };

        private object CreateCosmosReference<T>(string connectionString, string databaseName, string containerName)
        {
            Type targetType = typeof(T);
            CosmosClient cosmosClient = new (connectionString);

            object cosmosReference = targetType switch {
                Type _ when targetType == typeof(CosmosDatabase)    => cosmosClient.GetDatabase(databaseName),
                Type _ when targetType == typeof(CosmosContainer)   => cosmosClient.GetContainer(databaseName, containerName),
                _ => cosmosClient
            };

            return (T)cosmosReference;
        }
    }
}
