// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp
{
    public class CosmosFunction
    {
        private readonly ILogger<CosmosFunction> _logger;

        public CosmosFunction(ILogger<CosmosFunction> logger)
        {
            _logger = logger;
        }

        [Function(nameof(CosmosTrigger))]
        [CosmosDBOutput(
            databaseName: "%CosmosDb%",
            containerName: "%CosmosCollOut%",
            Connection = "CosmosConnection",
            CreateIfNotExists = true)]
        public object CosmosTrigger([CosmosDBTrigger(
            databaseName: "%CosmosDb%",
            containerName: "%CosmosCollIn%",
            Connection = "CosmosConnection",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)] IReadOnlyList<MyDocument> input, FunctionContext context)
        {
            if (input != null && input.Count > 0)
            {
                foreach (var doc in input)
                {
                    context.GetLogger("Function.CosmosTrigger").LogInformation($"id: {doc.Text}");
                }

                return input.Select(p => new { id = p.Text });
            }

            return null;
        }

        [Function(nameof(DocsByUsingCosmosClient))]
        public async Task<HttpResponseData>  DocsByUsingCosmosClient(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [CosmosDBInput("", "", Connection = "CosmosConnection")] CosmosClient client)
        {
            var container = client.GetContainer("ItemDb", "ItemCollectionIn");
            var iterator = container.GetItemQueryIterator<MyDocument>("SELECT * FROM c");

            var output = "";

            while (iterator.HasMoreResults)
            {
                var documents = await iterator.ReadNextAsync();
                foreach (dynamic d in documents)
                {
                    output += $"{(string)d.Text}, ";
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(output);
            return response;
        }

        [Function(nameof(DocsByUsingDatabaseClient))]
        public async Task<HttpResponseData> DocsByUsingDatabaseClient(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [CosmosDBInput("%CosmosDb%", "", Connection = "CosmosConnection")] Database database)
        {
            var container = database.GetContainer("ItemCollectionIn");;
            var iterator = container.GetItemQueryIterator<MyDocument>("SELECT * FROM c");

            var output = "";

            while (iterator.HasMoreResults)
            {
                var documents = await iterator.ReadNextAsync();
                foreach (dynamic d in documents)
                {
                    output += $"{(string)d.Text}, ";
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(output);
            return response;
        }

        [Function(nameof(DocsByUsingContainerClient))]
        public async Task<HttpResponseData>  DocsByUsingContainerClient(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [CosmosDBInput("%CosmosDb%", "%CosmosCollIn%", Connection = "CosmosConnection")] Container container)
        {
            var iterator = container.GetItemQueryIterator<MyDocument>("SELECT * FROM c");

            var output = "";

            while (iterator.HasMoreResults)
            {
                var documents = await iterator.ReadNextAsync();
                foreach (dynamic d in documents)
                {
                    output += $"{(string)d.Text}, ";
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(output);
            return response;
        }

        [Function(nameof(DocByIdFromRouteData))]
        public async Task<HttpResponseData> DocByIdFromRouteData(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "docsbyroute/{partitionKey}/{id}")] HttpRequestData req,
            [CosmosDBInput(
                databaseName: "%CosmosDb%",
                containerName: "%CosmosCollIn%",
                Connection = "CosmosConnection",
                Id = "{id}",
                PartitionKey = "{partitionKey}")] MyDocument doc)
        {
            if (doc == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(doc.Text);
            return response;
        }

        [Function(nameof(DocByIdFromRouteDataUsingSqlQuery))]
        public async Task<HttpResponseData> DocByIdFromRouteDataUsingSqlQuery(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "docsbysql/{id}")] HttpRequestData req,
            [CosmosDBInput(
                databaseName: "%CosmosDb%",
                containerName: "%CosmosCollIn%",
                Connection = "CosmosConnection",
                SqlQuery = "SELECT * FROM ItemDb t where t.id = {id}")]
                IEnumerable<MyDocument> myDocs)
        {
            var output = myDocs.FirstOrDefault().Text;
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(output);
            return response;
        }

        [Function(nameof(DocByIdFromQueryStringUsingSqlQuery))]
        public async Task<HttpResponseData> DocByIdFromQueryStringUsingSqlQuery(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [CosmosDBInput(
                databaseName: "%CosmosDb%",
                containerName: "%CosmosCollIn%",
                Connection = "CosmosConnection",
                SqlQuery = "SELECT * FROM ItemDb t where t.id = {id}")]
                MyDocument[] myDocs)
        {
            var output = myDocs.FirstOrDefault().Text;
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(output);
            return response;
        }

        public class MyDocument
        {
            public string Text { get; set; }
        }
    }
}
