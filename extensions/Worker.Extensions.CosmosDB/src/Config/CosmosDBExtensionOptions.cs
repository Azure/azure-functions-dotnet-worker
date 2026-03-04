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

        // TODO: In the next major version, ensure this defaults to WorkerOptions.Serializer.
        // This cannot be changed now to avoid breaking existing users who rely on the current default.
        // Currently, this defaults to DefaultSerializer via the CosmosDBBindingOptions.Serializer property,
        // unless UseCosmosDBWorkerSerializer is called.
        /// <summary>
        /// Gets or sets the ObjectSerializer used for deserializing CosmosDB POCOs.
        /// If not set, defaults to WorkerOptions.Serializer when UseCosmosDBWorkerSerializer is called.
        /// </summary>
        public ObjectSerializer? Serializer { get; set; }
    }
}
