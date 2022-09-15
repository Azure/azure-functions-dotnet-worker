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

            bindingData.Properties.TryGetValue("database_id", out var databaseId);
            bindingData.Properties.TryGetValue("container_id", out var containerId);
            bindingData.Properties.TryGetValue("connection_name", out var connectionName);
            var connectionString = Environment.GetEnvironmentVariable(connectionName);

            object result = ToTargetType(context.TargetType, connectionString, databaseId, containerId);

            if (result is not null)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Success(result));
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }

        private object? ToTargetType(Type targetType, string connectionString, string databaseId, string containerId) => targetType switch
        {
            Type _ when targetType == typeof(CosmosClient)      => CreateCosmosReference<CosmosClient>(connectionString, databaseId, containerId),
            Type _ when targetType == typeof(CosmosDatabase)    => CreateCosmosReference<CosmosDatabase>(connectionString, databaseId, containerId),
            Type _ when targetType == typeof(CosmosContainer)   => CreateCosmosReference<CosmosContainer>(connectionString, databaseId, containerId),
            _ => null
        };

        private object CreateCosmosReference<T>(string connectionString, string databaseId, string containerId)
        {
            Type targetType = typeof(T);
            CosmosClient cosmosClient = new (connectionString);

            object cosmosReference = targetType switch {
                Type _ when targetType == typeof(CosmosDatabase)    => cosmosClient.GetDatabase(databaseId),
                Type _ when targetType == typeof(CosmosContainer)   => cosmosClient.GetContainer(databaseId, containerId),
                _ => cosmosClient
            };

            return (T)cosmosReference;
        }
    }
}

// Example bindings from cosmos v3 + inproc
// [CosmosDBTrigger(
// databaseName: "databaseName",
// containerId: "containerId",
// Connection = "CosmosDBConnectionSetting",
// LeasecontainerId = "leases",
// CreateLeaseContainerIfNotExists = true)]

// [CosmosDB(
// databaseName: "ToDoItems",
// collectionName: "Items",
// ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client

// [CosmosDB(
// databaseName: "ToDoItems",
// containerId: "Items",
// Connection = "CosmosDBConnection")] CosmosClient client