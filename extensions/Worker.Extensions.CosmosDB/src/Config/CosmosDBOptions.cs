// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Cosmos;

namespace Microsoft.Azure.Functions.Worker
{
    public class CosmosDBOptions
    {
        /// <summary>
        /// Gets or sets the CosmosClientOptions.
        /// </summary>
        public CosmosClientOptions ClientOptions { get; set; } = new CosmosClientOptions();
    }
}