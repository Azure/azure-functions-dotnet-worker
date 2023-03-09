// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Azure.Functions.Worker
{
    internal class CosmosDBBindingOptions
    {
        public string? ConnectionString { get; set; }

        public string? AccountEndpoint { get; set; }

        public TokenCredential? Credential { get; set; }

        internal virtual CosmosClient CreateClient(CosmosClientOptions cosmosClientOptions)
        {
            return string.IsNullOrEmpty(ConnectionString)
                    ? new CosmosClient(AccountEndpoint, Credential, cosmosClientOptions) // AAD auth
                    : new CosmosClient(ConnectionString, cosmosClientOptions); // Connection string based auth
        }
    }
}