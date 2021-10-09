// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    public sealed class CosmosDBTriggerAttribute : TriggerBindingAttribute
    {
        /// <summary>
        /// Triggers an event when changes occur on a monitored container
        /// </summary>
        /// <param name="databaseName">Name of the database of the container to monitor for changes</param>
        /// <param name="containerName">Name of the container to monitor for changes</param>
        public CosmosDBTriggerAttribute(string databaseName, string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException("Missing information for the container to monitor", nameof(containerName));
            }

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentException("Missing information for the container to monitor", nameof(databaseName));
            }

            ContainerName = containerName;
            DatabaseName = databaseName;
        }

        /// <summary>
        /// Name of the container to monitor for changes
        /// </summary>
        public string ContainerName { get; private set; }

        /// <summary>
        /// Name of the database containing the container to monitor for changes
        /// </summary>
        public string DatabaseName { get; private set; }

        /// <summary>
        /// Optional.
        /// The name of the app setting containing your Azure Cosmos DB connection string.
        /// </summary>
        public string? Connection { get; set; }


        /// <summary>
        /// Name of the lease container. Default value is "leases"
        /// </summary>
        public string? LeaseContainerName { get; set; }

        /// <summary>
        /// Name of the database containing the lease container
        /// </summary>
        public string? LeaseDatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the connection string for the service containing the lease container.
        /// </summary>
        public string? LeaseConnection { get; set; }


        /// <summary>
        /// Optional.
        /// Only applies to lease container.
        /// If true, the database and container for leases will be automatically created if it does not exist.
        /// </summary>
        public bool CreateLeaseContainerIfNotExists { get; set; } = false;

        /// <summary>
        /// Optional.
        /// When specified on an output binding and <see cref="CreateLeaseContainerIfNotExists"/> is true, defines the throughput of the created
        /// container.
        /// </summary>
        public int LeasesContainerThroughput { get; set; }

        /// <summary>
        /// Optional.
        /// Defines a prefix to be used within a Leases container for this Trigger. Useful when sharing the same Lease container among multiple Triggers
        /// </summary>
        public string? LeaseContainerPrefix { get; set; }
        
        /// <summary>
        /// Optional.
        /// Customizes the delay in milliseconds in between polling a partition for new changes on the feed, after all current changes are drained.  Default is 5000 (5 seconds).
        /// </summary>
        public int FeedPollDelay { get; set; }

        /// <summary>
        /// Optional.
        /// Customizes the renew interval in milliseconds for all leases for partitions currently held by the Trigger. Default is 17000 (17 seconds).
        /// </summary>
        public int LeaseRenewInterval { get; set; }

        /// <summary>
        /// Optional.
        /// Customizes the interval in milliseconds to kick off a task to compute if partitions are distributed evenly among known host instances. Default is 13000 (13 seconds).
        /// </summary>
        public int LeaseAcquireInterval { get; set; }

        /// <summary>
        /// Optional.
        /// Customizes the interval in milliseconds for which the lease is taken on a lease representing a partition. If the lease is not renewed within this interval, it will cause it to expire and ownership of the partition will move to another Trigger instance. Default is 60000 (60 seconds).
        /// </summary>
        public int LeaseExpirationInterval { get; set; }

        /// <summary>
        /// Optional.
        /// Customizes the maximum amount of items received in an invocation
        /// </summary>
        public int MaxItemsPerInvocation { get; set; }

        /// <summary>
        /// Optional.
        /// Gets or sets whether change feed in the Azure Cosmos DB service should start from beginning (true) or from current (false). By default it's start from current (false).
        /// </summary>
        public bool StartFromBeginning { get; set; } = false;

        /// <summary>
        /// Optional.
        /// GGets or sets the a date and time to initialize the change feed read operation from. 
        /// The recommended format is ISO 8601 with the UTC designator. 
        /// For example: "2021-02-16T14:19:29Z"
        /// </summary>
        public bool? StartFromTime { get; set; }

        /// <summary>
        /// Optional.
        /// Defines preferred locations (regions) for geo-replicated database accounts in the Azure Cosmos DB service.
        /// Values should be comma-separated.
        /// </summary>
        /// <example>
        /// PreferredLocations = "East US,South Central US,North Europe".
        /// </example>
        public string? PreferredLocations { get; set; }
    }
}
