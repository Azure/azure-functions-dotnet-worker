// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net;
using System.Text;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Cosmos;
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [TableInput("TableName")] TableClient table)

        {
            var tableEntity = table.QueryAsync<TableEntity>();
            var response = req.CreateResponse(HttpStatusCode.OK);

            List<string> tableList = new();
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "items/{partitionKey}/{rowKey}")] HttpRequestData req,
            [TableInput("TableName", "{partitionKey}", "{rowKey}")] TableEntity table)

        {
            table.TryGetValue("Text", out var text);
            var response =  req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(text?.ToString() ?? "");
            return response;
        }

        [Function(nameof(ReadTableDataFunctionWithFilter))]
        public async Task<HttpResponseData> ReadTableDataFunctionWithFilter(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [TableInput("TableName", "My Partition", Filter = "RowKey ne 'value'", Take = 2, IsBatched = true)] IEnumerable<TableEntity> table)

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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "items/{partitionKey}")] HttpRequestData req,
            [TableInput("TableName", "{partitionKey}", IsBatched = true)] IEnumerable<TableEntity> tables)

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
    }
}

