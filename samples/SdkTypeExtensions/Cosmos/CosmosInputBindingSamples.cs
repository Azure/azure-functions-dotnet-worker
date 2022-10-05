// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using Azure.Cosmos;
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

        [Function(nameof(DocsByUsingCosmosClient))]
        public void DocsByUsingCosmosClient(
            [BlobTrigger("cosmosdemo/{name}", Connection = "AzureWebJobsStorage")] string data,
            [CosmosDBInput("testdb", "inputcontainer", Connection = "CosmosDBConnection")] CosmosClient client,
            FunctionContext context)
        {
            var downloadResult = client.Endpoint.AbsoluteUri;
            _logger.LogInformation("Cosmos endpoint: {uri}", downloadResult);
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [CosmosDBInput(
                databaseName: "ToDoItems",
                containerName: "Items",
                Connection = "CosmosDBConnection",
                SqlQuery = "SELECT top 2 * FROM c order by c._ts desc")] IEnumerable<ToDoItem> toDoItems)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            foreach (ToDoItem toDoItem in toDoItems)
            {
                _logger.LogInformation(toDoItem.Description);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
