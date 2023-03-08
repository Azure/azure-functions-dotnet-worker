// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    public sealed class CosmosDBOutputAttribute : OutputBindingAttribute
    {
        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="databaseName">The CosmosDB database name.</param>
        /// <param name="containerName">The CosmosDB container name.</param>
        public CosmosDBOutputAttribute(string databaseName, string containerName)
        {
            DatabaseName = databaseName;
            ContainerName = containerName;
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
        public string ContainerName { get; private set; }

        /// <summary>
        /// Optional.
        /// The name of the app setting containing your Azure Cosmos DB connection string.
        /// </summary>
        public string? Connection { get; set; }

        /// <summary>
        /// Optional.
        /// Only applies to output bindings.
        /// If true, the database and container will be automatically created if they do not exist.
        /// </summary>
        public bool CreateIfNotExists { get; set; }

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
        /// When specified on an output binding and <see cref="CreateIfNotExists"/> is true, defines the throughput of the created
        /// container.
        /// </summary>
        public int ContainerThroughput { get; set; }

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
