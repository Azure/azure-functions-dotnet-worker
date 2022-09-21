// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class CosmosClientFunction
    {
        [Function(nameof(CosmosClientFunction))]
        public static async Task Run(
            [CosmosDBTrigger("testdb", "testcontainer", Connection = "CosmosDBConnection", CreateLeaseContainerIfNotExists = true)] CosmosClient client,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(CosmosClientFunction));
            var downloadResult = client.Endpoint.AbsoluteUri;
            logger.LogInformation("Cosmos endpoint: {uri}", downloadResult);
        }
    }
}
