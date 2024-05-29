// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    [InputConverter(typeof(CosmosDBMongoConverter))]
    [ConverterFallbackBehavior(ConverterFallbackBehavior.Default)]
    public sealed class CosmosDBMongoInputAttribute : InputBindingAttribute
    {
        /// <summary>
        /// Constructs a new instance.
        /// Use this constructor when binding to a CosmosClient.
        /// </summary>
        public CosmosDBMongoInputAttribute()
        {
            DatabaseName = string.Empty;
            CollectionName = string.Empty;
        }

        /// <summary>
        /// Constructs a new instance with the specified database name.
        /// Use this constructor when binding to a Database.
        /// </summary>
        /// <param name="databaseName">The CosmosDB database name.</param>
        public CosmosDBMongoInputAttribute(string databaseName)
        {
            DatabaseName = databaseName;
            CollectionName = string.Empty;
        }

        /// <summary>
        /// Constructs a new instance with the specified database and container names.
        /// Use this constructor when binding to a Container or a POCO.
        /// </summary>
        /// <param name="databaseName">The CosmosDB database name.</param>
        /// <param name="collectionName">The CosmosDB container name.</param>
        [JsonConstructor]
        public CosmosDBMongoInputAttribute(string databaseName, string collectionName)
        {
            DatabaseName = databaseName;
            CollectionName = collectionName;
        }

        /// <summary>
        /// The name of the database to which the parameter applies.
        /// May include binding parameters.
        /// </summary>
        public string DatabaseName { get; private set; }

        /// <summary>
        /// The name of the container to which the parameter applies.
        /// May include binding parameters.
        /// </summary>
        public string CollectionName { get; private set; }

        /// <summary>
        /// Optional.
        /// The name of the app setting containing your Azure Cosmos DB connection string.
        /// </summary>
        public string? ConnectionStringKey { get; set; }

        /// <summary>
        /// Optional. The Id of the document to retrieve from the container.
        /// May include binding parameters.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Optional.
        /// When specified on an output binding and <see cref="CreateIfNotExists"/> is true, defines the partition key
        /// path for the created container.
        /// When specified on an input binding, specifies the partition key value for the lookup.
        /// May include binding parameters.
        /// </summary>
        public string? PartitionKey { get; set; }

        /// <summary>
        /// Optional.
        /// When specified on an input binding using an <see cref="System.Collections.Generic.IEnumerable{T}"/>, defines the query to run against the collection.
        /// May include binding parameters.
        /// </summary>
        public string? SqlQuery { get; set; }

        /// <summary>
        /// Optional.
        /// Defines preferred locations (regions) for geo-replicated database accounts in the Azure Cosmos DB service.
        /// Values should be comma-separated.
        /// </summary>
        /// <example>
        /// PreferredLocations = "East US,South Central US,North Europe"
        /// </example>
        public string? PreferredLocations { get; set; }

        /// <summary>
        /// Optional.
        /// Defines the parameters to be used with the SqlQuery
        /// </summary>
        public IDictionary<string, object>? SqlQueryParameters { get; set; }
    }
}
