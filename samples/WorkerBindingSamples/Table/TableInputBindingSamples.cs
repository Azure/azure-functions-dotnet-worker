// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace WorkerBindingSamples.Table
{
    public class TableInputBindingSamples
    {
        private readonly ILogger<TableInputBindingSamples> _logger;

        public TableInputBindingSamples(ILogger<TableInputBindingSamples> logger)
        {
            _logger = logger;
        }

        [Function(nameof(TableClientFunction))]
        public async Task<HttpResponseData> TableClientFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [TableInput("TableName")] TableClient table)
        {
            var tableEntity = table.QueryAsync<TableEntity>();
            var response = req.CreateResponse(HttpStatusCode.OK);

            await foreach (TableEntity val in tableEntity)
            {
                val.TryGetValue("Text", out var text);
                _logger.LogInformation("Value of text: " + text);

                await response.WriteStringAsync(text?.ToString() ?? "");
            }

            return response;
        }

        [Function(nameof(ReadTableDataFunction))]
        public async Task<HttpResponseData> ReadTableDataFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "items/{partitionKey}/{rowKey}")] HttpRequestData req,
            [TableInput("TableName", "{partitionKey}", "{rowKey}")] TableEntity table)

        {
            table.TryGetValue("Text", out var text);
            var response =  req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(text?.ToString() ?? "");
            return response;
        }

        [Function(nameof(ReadTableDataFunctionWithFilter))]
        public async Task<HttpResponseData> ReadTableDataFunctionWithFilter(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [TableInput("TableName", "My Partition", 2, Filter = "RowKey ne 'value'")] IEnumerable<TableEntity> table)

        {
            List<string> tableList = new();
            var response = req.CreateResponse(HttpStatusCode.OK);

            foreach (TableEntity tableEntity in table)
            {
                tableEntity.TryGetValue("Text", out var text);
                _logger.LogInformation("Value of text: " + text);
                tableList.Add(text?.ToString() ?? "");
            }

            await response.WriteStringAsync(string.Join(",", tableList));
            return response;
        }

        [Function(nameof(EnumerableFunction))]
        public async Task<HttpResponseData> EnumerableFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "items/{partitionKey}")] HttpRequestData req,
            [TableInput("TableName", "{partitionKey}")] IEnumerable<TableEntity> tables)

        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            List<string> tableList = new();

            foreach (TableEntity tableEntity in tables)
            {
                tableEntity.TryGetValue("Text", out var text);
                _logger.LogInformation("Value of text: " + text);
                tableList.Add((text?.ToString()) ?? "");
            }

            await response.WriteStringAsync(string.Join(",", tableList));
            return response;
        }

        [Function(nameof(PocoFunction))]
        public async Task<HttpResponseData> PocoFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get","post", Route = null)] HttpRequestData req,
            [TableInput("TableName")] IEnumerable<MyEntity> entities,
            FunctionContext executionContext)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            List<string> entityList = new();

            foreach (MyEntity entity in entities)
            {
                _logger.LogInformation($"Text: {entity.Text}");
                entityList.Add((entity.Text ?? "").ToString());
            }

            await response.WriteStringAsync(string.Join(",", entityList));
            return response;
        }
    }

    public class MyEntity
    {
        public string? Text { get; set; }
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
    }
}
