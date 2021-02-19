// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.CosmosDB
{
    public sealed class CosmosDBOutputAttribute : OutputBindingAttribute
    {
        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="databaseName">The CosmosDB database name.</param>
        /// <param name="collectionName">The CosmosDB collection name.</param>
        public CosmosDBOutputAttribute(string databaseName, string collectionName)
        {
            DatabaseName = databaseName;
            CollectionName = collectionName;
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="databaseName">The CosmosDB database name.</param>
        /// <param name="collectionName">The CosmosDB collection name.</param>
        public CosmosDBOutputAttribute(string name, string databaseName, string collectionName) : base(name)
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
        /// The name of the collection to which the parameter applies.
        /// May include binding parameters.
        /// </summary>
        public string CollectionName { get; private set; }

        /// <summary>
        /// Optional.
        /// Only applies to output bindings.
        /// If true, the database and collection will be automatically created if they do not exist.
        /// </summary>
        public bool CreateIfNotExists { get; set; }

        /// <summary>
        /// Optional. A string value indicating the app setting to use as the CosmosDB connection string, if different
        /// than the one specified in the <see cref="CosmosDBOptions"/>.
        /// </summary>
        public string? ConnectionStringSetting { get; set; }

        /// <summary>
        /// Optional.
        /// When specified on an output binding and <see cref="CreateIfNotExists"/> is true, defines the partition key 
        /// path for the created collection.
        /// When specified on an input binding, specifies the partition key value for the lookup.
        /// May include binding parameters.
        /// </summary>
        public string? PartitionKey { get; set; }

        /// <summary>
        /// Optional.
        /// When specified on an output binding and <see cref="CreateIfNotExists"/> is true, defines the throughput of the created
        /// collection.
        /// </summary>
        public int CollectionThroughput { get; set; }

        /// <summary>
        /// Optional.
        /// Enable to use with Multi Master accounts.
        /// </summary>
        public bool UseMultipleWriteLocations { get; set; }

        /// <summary>
        /// Optional.
        /// Defines preferred locations (regions) for geo-replicated database accounts in the Azure Cosmos DB service.
        /// Values should be comma-separated.
        /// </summary>
        /// <example>
        /// PreferredLocations = "East US,South Central US,North Europe"
        /// </example>
        public string? PreferredLocations { get; set; }
    }
}
