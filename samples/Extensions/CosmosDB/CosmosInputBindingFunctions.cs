// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    /// <summary>
    /// Samples demonstrating binding to the <see cref="CosmosClient"/>, <see cref="Database"/>, and <see cref="Container"/> types.
    /// </summary>
    public class CosmosInputBindingFunctions
    {
        private readonly ILogger<CosmosInputBindingFunctions> _logger;

        public CosmosInputBindingFunctions(ILogger<CosmosInputBindingFunctions> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// This sample demonstrates how to retrieve a collection of documents.
        /// The code uses a <see cref="CosmosClient"/> instance to read a list of documents.
        /// The <see cref="CosmosClient"/> instance could also be used for write operations.
        /// </summary>
        [Function(nameof(DocsByUsingCosmosClient))]
        public async Task<HttpResponseData> DocsByUsingCosmosClient(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [CosmosDBInput(Connection = "CosmosDBConnection")] CosmosClient client)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var iterator = client.GetContainer("ToDoItems", "Items")
                                 .GetItemQueryIterator<ToDoItem>("SELECT * FROM c");

            while (iterator.HasMoreResults)
            {
                var documents = await iterator.ReadNextAsync();
                foreach (ToDoItem item in documents)
                {
                    _logger.LogInformation(item.Description);
                }
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// This sample demonstrates how to retrieve a collection of documents.
        /// The function is triggered by an HTTP request and binds to the specified database.
        /// as a <see cref="Database"/> type. The function then queries for all collections in the database.
        /// </summary>
        [Function(nameof(DocsByUsingDatabaseClient))]
        public async Task<HttpResponseData> DocsByUsingDatabaseClient(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [CosmosDBInput("ToDoItems", Connection = "CosmosDBConnection")] Database database)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var iterator = database.GetContainerQueryIterator<ContainerProperties>("SELECT * FROM c");

            while (iterator.HasMoreResults)
            {
                var containers = await iterator.ReadNextAsync();
                foreach (ContainerProperties c in containers)
                {
                    _logger.LogInformation(c.Id);
                }
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// This sample demonstrates how to retrieve a collection of documents.
        /// The function is triggered by an HTTP request and binds to the specified database and collection
        /// as a <see cref="Container"/> type. The function then queries for all documents in the collection.
        /// </summary>
        [Function(nameof(DocsByUsingContainerClient))]
        public async Task<HttpResponseData> DocsByUsingContainerClient(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [CosmosDBInput("ToDoItems", "Items", Connection = "CosmosDBConnection")] Container container)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var iterator = container.GetItemQueryIterator<ToDoItem>("SELECT * FROM c");

            while (iterator.HasMoreResults)
            {
                var documents = await iterator.ReadNextAsync();
                foreach (ToDoItem item in documents)
                {
                    _logger.LogInformation("Found ToDo item, Description={desc}", item.Description);
                }
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// This sample demonstrates how to retrieve a single document.
        /// The function is triggered by an HTTP request that uses a query string to specify the ID and partition key value to look up.
        /// That ID and partition key value are used to retrieve a ToDoItem document from the specified database and collection.
        /// </summary>
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

        /// <summary>
        /// This sample demonstrates how to retrieve a single document.
        /// The function is triggered by an HTTP request that uses route data to specify the ID and partition key value to look up.
        /// That ID and partition key value are used to retrieve a ToDoItem document from the specified database and collection.
        /// </summary>
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

        /// <summary>
        /// This sample demonstrates how to retrieve a collection of documents.
        /// The function is triggered by an HTTP request that uses route data to specify the ID to look up.
        /// That ID is used to retrieve a list of ToDoItem documents from the specified database and collection.
        /// The example shows how to use a binding expression in the <see cref="CosmosDBInputAttribute.SqlQuery"/> parameter.
        /// </summary>
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

        /// <summary>
        /// This sample demonstrates how to retrieve a collection of documents.
        /// The function is triggered by an HTTP request that uses a query string to specify the ID to look up.
        /// That ID is used to retrieve a list of ToDoItem documents from the specified database and collection.
        /// The example shows how to use a binding expression in the <see cref="CosmosDBInputAttribute.SqlQuery"/> parameter.
        /// </summary>
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

        /// <summary>
        /// This sample demonstrates how to retrieve a collection of documents.
        /// The function is triggered by an HTTP request. The query is specified in the <see cref="CosmosDBInputAttribute.SqlQuery"/> attribute property.
        /// </summary>
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

        /// <summary>
        /// This sample demonstrates how to retrieve a single document.
        /// The function is triggered by a queue message that contains a JSON object. The queue trigger parses the JSON into
        /// an object of type ToDoItemLookup, which contains the ID and partition key value to look up. That ID and partition
        /// key value are used to retrieve a ToDoItem document from the specified database and collection.
        /// </summary>
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

        public class ToDoItem
        {
            public string Id { get; set; }
            public string Description { get; set; }
        }

        public class ToDoItemLookup
        {
            public string ToDoItemId { get; set; }

            public string ToDoItemPartitionKeyValue { get; set; }
        }
    }
}
