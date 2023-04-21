// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net;
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

        [Function(nameof(DocsByUsingCosmosClient))]
        public async Task<HttpResponseData>  DocsByUsingCosmosClient(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [CosmosDBInput("", "", Connection = "CosmosDBConnection")] CosmosClient client)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var iterator = client.GetContainer("ToDoItems", "Items")
                                 .GetItemQueryIterator<dynamic>("SELECT * FROM c");

            while (iterator.HasMoreResults)
            {
                var documents = await iterator.ReadNextAsync();
                foreach (dynamic d in documents)
                {
                    _logger.LogInformation((string)d.description);
                }
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(DocsByUsingDatabaseClient))]
        public async Task<HttpResponseData> DocsByUsingDatabaseClient(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [CosmosDBInput("ToDoItems", "", Connection = "CosmosDBConnection")] Database database)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var iterator = database.GetContainerQueryIterator<dynamic>("SELECT * FROM c");

            while (iterator.HasMoreResults)
            {
                var containers = await iterator.ReadNextAsync();
                foreach (dynamic c in containers)
                {
                    _logger.LogInformation((string)c.id);
                }
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(DocsByUsingContainerClient))]
        public async Task<HttpResponseData>  DocsByUsingContainerClient(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [CosmosDBInput("ToDoItems", "Items", Connection = "CosmosDBConnection")] Container container)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var iterator = container.GetItemQueryIterator<dynamic>("SELECT * FROM c");

            while (iterator.HasMoreResults)
            {
                var documents = await iterator.ReadNextAsync();
                foreach (dynamic d in documents)
                {
                    _logger.LogInformation("Found ToDo item, Description={desc}", (string)d.description);
                }
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(DocByIdFromQueryString))]
        public HttpResponseData DocByIdFromQueryString(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [CosmosDBInput(
                databaseName: "ToDoItems",
                containerName: "Items",
                Connection = "CosmosDBConnection",
                Id = "{Query.id}",
                PartitionKey = "{Query.partitionKey}")] ToDoItem toDoItem)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            if (toDoItem == null)
            {
                _logger.LogInformation("ToDo item not found");
            }
            else
            {
                _logger.LogInformation("Found ToDo item, Description={desc}", toDoItem.Description);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(DocByIdFromRouteData))]
        public HttpResponseData DocByIdFromRouteData(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "todoitems/{partitionKey}/{id}")] HttpRequestData req,
            [CosmosDBInput(
                databaseName: "ToDoItems",
                containerName: "Items",
                Connection = "CosmosDBConnection",
                Id = "{id}",
                PartitionKey = "{partitionKey}")] ToDoItem toDoItem)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

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

        [Function(nameof(DocByIdFromRouteDataUsingSqlQuery))]
        public HttpResponseData DocByIdFromRouteDataUsingSqlQuery(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "todoitems2/{id}")] HttpRequestData req,
            [CosmosDBInput(
                databaseName: "ToDoItems",
                containerName: "Items",
                Connection = "CosmosDBConnection",
                SqlQuery = "SELECT * FROM ToDoItems t where t.id = {id}")]
                IEnumerable<ToDoItem> toDoItems)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            foreach (ToDoItem toDoItem in toDoItems)
            {
                _logger.LogInformation(toDoItem.Description);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(DocByIdFromQueryStringUsingSqlQuery))]
        public HttpResponseData DocByIdFromQueryStringUsingSqlQuery(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [CosmosDBInput(
                databaseName: "ToDoItems",
                containerName: "Items",
                Connection = "CosmosDBConnection",
                SqlQuery = "SELECT * FROM ToDoItems t where t.id = {id}")]
                IEnumerable<ToDoItem> toDoItems)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            foreach (ToDoItem toDoItem in toDoItems)
            {
                _logger.LogInformation(toDoItem.Description);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(DocsBySqlQuery))]
        public HttpResponseData DocsBySqlQuery(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [CosmosDBInput(
                databaseName: "ToDoItems",
                containerName: "Items",
                Connection = "CosmosDBConnection",
                SqlQuery = "SELECT * FROM ToDoItems t WHERE CONTAINS(t.description, 'cat')")] IEnumerable<ToDoItem> toDoItems)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            foreach (ToDoItem toDoItem in toDoItems)
            {
                _logger.LogInformation(toDoItem.Description);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(DocByIdFromJSON))]
        public void DocByIdFromJSON(
            [QueueTrigger("todoqueueforlookup")] ToDoItemLookup toDoItemLookup,
            [CosmosDBInput(
                databaseName: "ToDoItems",
                containerName: "Items",
                Connection  = "CosmosDBConnection",
                Id = "{ToDoItemId}",
                PartitionKey = "{ToDoItemPartitionKeyValue}")] ToDoItem toDoItem)
        {
            _logger.LogInformation($"C# Queue trigger function processed Id={toDoItemLookup?.ToDoItemId} Key={toDoItemLookup?.ToDoItemPartitionKeyValue}");

            if (toDoItem == null)
            {
                _logger.LogInformation($"ToDo item not found");
            }
            else
            {
                _logger.LogInformation($"Found ToDo item, Description={toDoItem.Description}");
            }
        }

        public class ToDoItemLookup
        {
            public string? ToDoItemId { get; set; }

            public string? ToDoItemPartitionKeyValue { get; set; }
        }
    }
}
