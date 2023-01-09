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
    public class CosmosInputBindingFunctions
    {
        private readonly ILogger<CosmosInputBindingFunctions> _logger;

        public CosmosInputBindingFunctions(ILogger<CosmosInputBindingFunctions> logger)
        {
            _logger = logger;
        }

        // Note: attribute should not require databaseName and containerName for CosmosClient
        [Function(nameof(DocsByCosmosClient))]
        public async Task DocsByCosmosClient(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [CosmosDBInput("ToDoItems", "Items", Connection = "CosmosDBConnection")] CosmosClient client,
            FunctionContext context)
        {
            _logger.LogInformation("DocsByCosmosClient function triggered");

            var iterator = client.GetContainer("ToDoItems", "Items")
                                 .GetItemQueryIterator<dynamic>("SELECT * FROM c");

            while (iterator.HasMoreResults)
            {
                var documents = await iterator.ReadNextAsync();
                foreach (dynamic d in documents)
                {
                    Console.WriteLine(d.description);
                }
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        // Note: attribute should not require containerName for Database
        [Function(nameof(DocsByDatabaseClient))]
        public async Task DocsByDatabaseClient(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [CosmosDBInput("ToDoItems", "Items", Connection = "CosmosDBConnection")] Database database,
            FunctionContext context)
        {
            _logger.LogInformation("DocsByDatabaseClient function triggered");

            var iterator = database.GetContainerQueryIterator<dynamic>("SELECT * FROM c");

            while (iterator.HasMoreResults)
            {
                var containers = await iterator.ReadNextAsync();
                foreach (dynamic c in containers)
                {
                    Console.WriteLine(c.id);
                }
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(DocsByContainerClient))]
        public async Task DocsByContainerClient(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [CosmosDBInput("ToDoItems", "Items", Connection = "CosmosDBConnection")] Container container,
            FunctionContext context)
        {
            _logger.LogInformation("DocsByContainerClient function triggered");

            var iterator = container.GetItemQueryIterator<dynamic>("SELECT * FROM c");

            while (iterator.HasMoreResults)
            {
                var documents = await iterator.ReadNextAsync();
                foreach (dynamic d in documents)
                {
                    Console.WriteLine(d.description);
                }
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(DocsUsingSqlQuery))]
        public HttpResponseData DocsUsingSqlQuery(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [CosmosDBInput(
                databaseName: "ToDoItems",
                containerName: "Items",
                Connection = "CosmosDBConnection",
                SqlQuery = "SELECT * FROM s WHERE CONTAINS(s.description, 'cat')")] IEnumerable<ToDoItem> toDoItems)
        {
            _logger.LogInformation("DocsUsingSqlQuery function triggered");

            foreach (ToDoItem toDoItem in toDoItems)
            {
                _logger.LogInformation(toDoItem.Description);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(DocByIdFromRouteData))]
        public HttpResponseData DocByIdFromRouteData(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "todoitems/{partitionKey}/{id}")] HttpRequestData req,
            [CosmosDBInput(
                databaseName: "ToDoItems",
                containerName: "Items",
                Connection = "CosmosDBConnection",
                Id = "{id}",
                PartitionKey = "{partitionKey}")] ToDoItem toDoItem)
        {
            _logger.LogInformation("DocByIdFromRouteData function triggered");

            if (toDoItem == null)
            {
                _logger.LogInformation($"ToDo item not found");
            }
            else
            {
                _logger.LogInformation($"Found ToDo item, Description={toDoItem.Description}");
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(DocByIdFromQueryString))]
        public HttpResponseData DocByIdFromQueryString(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [CosmosDBInput(
                databaseName: "ToDoItems",
                containerName: "Items",
                Connection = "CosmosDBConnection",
                Id = "{Query.id}",
                PartitionKey = "{Query.partitionKey}")] ToDoItem toDoItem)
        {
            _logger.LogInformation("DocByIdFromQueryString function triggered");

            if (toDoItem == null)
            {
                _logger.LogInformation($"ToDo item not found");
            }
            else
            {
                _logger.LogInformation($"Found ToDo item, Description={toDoItem.Description}");
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
