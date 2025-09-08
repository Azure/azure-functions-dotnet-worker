// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Azure.Functions.Worker
{
    public class CosmosDBExtensionOptions
    {
        /// <summary>
        /// Gets or sets the CosmosClientOptions.
        /// </summary>
        public CosmosClientOptions ClientOptions { get; set; } = new() { ConnectionMode = ConnectionMode.Gateway };

        // TODO: in next major version, ensure this is WorkerOptions.Serializer by default.
        public ObjectSerializer? Serializer { get; set; }
    }
}
