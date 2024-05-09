// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Cosmos;

namespace Microsoft.Azure.Functions.Worker
{
    public class CosmosDBOptions
    {
        /// <summary>
        /// Gets or sets the ConnectionMode used in the CosmosClient instances.
        /// </summary>
        /// <remarks>Default is Gateway mode.</remarks>
        public ConnectionMode? ConnectionMode { get; set; }

        /// <summary>
        /// Gets or sets a string to be included in the User Agent for all operations by Cosmos DB bindings and triggers.
        /// </summary>
        public string? UserAgentSuffix { get; set; }

        public CosmosDBOptions()
        {
            ConnectionMode = Cosmos.ConnectionMode.Gateway;
        }
    }
}