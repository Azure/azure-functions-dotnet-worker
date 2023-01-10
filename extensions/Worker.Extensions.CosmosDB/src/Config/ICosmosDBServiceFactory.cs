// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Cosmos;

namespace Microsoft.Azure.Functions.Worker
{
    internal interface ICosmosDBServiceFactory
    {
        CosmosClient CreateService(string connectionString, CosmosClientOptions cosmosClientOptions);
    }
}
