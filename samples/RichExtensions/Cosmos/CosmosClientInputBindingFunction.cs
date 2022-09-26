// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class CosmosClientInputBindingFunction
    {
        [Function(nameof(CosmosClientInputBindingFunction))]
        public static async Task Run(
            [BlobTrigger("blobstring-trigger/{name}", Connection = "AzureWebJobsStorage")] string data,
            [CosmosDBInput("testdb", "testcontainer", Connection = "CosmosDBConnection")] CosmosClient client,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(CosmosClientInputBindingFunction));
            var downloadResult = client.Endpoint.AbsoluteUri;
            logger.LogInformation("Cosmos endpoint: {uri}", downloadResult);
        }
    }
}
