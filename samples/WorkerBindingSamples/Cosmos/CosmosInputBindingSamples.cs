// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public class CosmosInputBindingSamples
    {
        private readonly ILogger<CosmosInputBindingSamples> _logger;

        public CosmosInputBindingSamples(ILogger<CosmosInputBindingSamples> logger)
        {
            _logger = logger;
        }

        // Note: attribute should not require databaseName and containerName for CosmosClient
        [Function(nameof(CosmosClientFunction))]
        public async Task CosmosClientFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [CosmosDBInput("testdb", "inputcontainer", Connection = "CosmosDBConnection")] CosmosClient client,
            FunctionContext context)
        {
            _logger.LogInformation("CosmosClientFunction triggered");

            var iterator = client.GetContainer("testdb", "inputcontainer")
                                 .GetItemQueryIterator<dynamic>("SELECT * FROM c");

            while (iterator.HasMoreResults)
            {
                var documents = await iterator.ReadNextAsync();
                foreach (dynamic d in documents)
                {
                    Console.WriteLine(d.id);
                }
            }
        }

        // Note: attribute should not require  containerName for Database
        [Function(nameof(CosmosDatabaseFunction))]
        public async Task CosmosDatabaseFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [CosmosDBInput("testdb", "inputcontainer", Connection = "CosmosDBConnection")] Database database,
            FunctionContext context)
        {
            _logger.LogInformation("CosmosDatabaseFunction triggered");

            var iterator = database.GetContainerQueryIterator<dynamic>("SELECT * FROM c");

            while (iterator.HasMoreResults)
            {
                var containers = await iterator.ReadNextAsync();
                foreach (dynamic c in containers)
                {
                    Console.WriteLine(c.id);
                }
            }
        }

        [Function(nameof(CosmosContainerFunction))]
        public async Task CosmosContainerFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [CosmosDBInput("testdb", "inputcontainer", Connection = "CosmosDBConnection")] Container container,
            FunctionContext context)
        {
            _logger.LogInformation("CosmosContainerFunction triggered");

            var iterator = container.GetItemQueryIterator<dynamic>("SELECT * FROM c");

            while (iterator.HasMoreResults)
            {
                var documents = await iterator.ReadNextAsync();
                foreach (dynamic d in documents)
                {
                    Console.WriteLine(d.id);
                }
            }
        }

        [Function(nameof(CosmosPOCOFunction))]
        public void CosmosPOCOFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [CosmosDBInput("testdb",
                            "inputcontainer",
                            Id = "1",
                            PartitionKey = "1",
                            Connection = "CosmosDBConnection")] ToDoItem item,
            FunctionContext context)
        {
            _logger.LogInformation("CosmosPOCOFunction triggered");
            _logger.LogInformation("{id}: {desc}", item.Id, item.Description);
        }

        [Function(nameof(CosmosPOCOCollectionFunction))]
        public void CosmosPOCOCollectionFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [CosmosDBInput("testdb", "inputcontainer", Connection = "CosmosDBConnection")] IReadOnlyList<ToDoItem> todoItems,
            FunctionContext context)
        {
            _logger.LogInformation("CosmosPOCOCollectionFunction triggered");

            if (todoItems is not null && todoItems.Any())
            {
                foreach (var item in todoItems)
                {
                    _logger.LogInformation("{id}: {desc}", item.Id, item.Description);
                }
            }
        }

        [Function(nameof(CosmosPOCOCollectionWithQueryFunction))]
        public void CosmosPOCOCollectionWithQueryFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [CosmosDBInput("testdb", "inputcontainer", Connection = "CosmosDBConnection",
                            SqlQuery = "SELECT * FROM s WHERE CONTAINS(s.description, 'cat')")] IReadOnlyList<ToDoItem> todoItems,
            FunctionContext context)
        {
            _logger.LogInformation("CosmosPOCOCollectionFunction triggered");

            if (todoItems is not null && todoItems.Any())
            {
                foreach (var item in todoItems)
                {
                    _logger.LogInformation("{id}: {desc}", item.Id, item.Description);
                }
            }
        }

        [Function(nameof(CosmosClientWithBlobTrigger))]
        public async Task CosmosClientWithBlobTrigger(
            [BlobTrigger("cosmosdemo/{name}", Connection = "AzureWebJobsStorage")] string data,
            [CosmosDBInput("testdb", "inputcontainer", Connection = "CosmosDBConnection")] CosmosClient client,
            FunctionContext context)
        {
            _logger.LogInformation("CosmosClientFunction triggered");

            var iterator = client.GetContainer("testdb", "inputcontainer")
                                 .GetItemQueryIterator<dynamic>("SELECT * FROM c");

            while (iterator.HasMoreResults)
            {
                var documents = await iterator.ReadNextAsync();
                foreach (dynamic d in documents)
                {
                    Console.WriteLine(d.id);
                }
            }
        }
    }
}
