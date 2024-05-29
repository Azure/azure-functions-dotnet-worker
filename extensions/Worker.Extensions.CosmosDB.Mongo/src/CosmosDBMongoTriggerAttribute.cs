// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    [BindingCapabilities(KnownBindingCapabilities.FunctionLevelRetry)]
    public sealed class CosmosDBMongoTriggerAttribute : TriggerBindingAttribute
    {
        /// <summary>
        /// Triggers an event when changes occur on a monitored container
        /// </summary>
        /// <param name="databaseName">Name of the database of the container to monitor for changes</param>
        /// <param name="collectionName">Name of the container to monitor for changes</param>
        public CosmosDBMongoTriggerAttribute(string databaseName, string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                throw new ArgumentException("Missing information for the container to monitor", nameof(collectionName));
            }

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentException("Missing information for the container to monitor", nameof(databaseName));
            }

            CollectionName = collectionName;
            DatabaseName = databaseName;
        }

        /// <summary>
        /// Name of the container to monitor for changes
        /// </summary>
        public string CollectionName { get; private set; }

        /// <summary>
        /// Name of the database containing the container to monitor for changes
        /// </summary>
        public string DatabaseName { get; private set; }

        /// <summary>
        /// Optional.
        /// The name of the app setting containing your Azure Cosmos DB connection string.
        /// </summary>
        public string? ConnectionStringKey { get; set; }

        public string? LeaseConnectionStringKey { get; set; }

        public string LeaseDatabaseName { get; set; }

        public string LeaseCollectionName { get; set; }
    }
}
