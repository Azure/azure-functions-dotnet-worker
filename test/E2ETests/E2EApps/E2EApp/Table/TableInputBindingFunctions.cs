﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp.Table
{
    public class TableInputBindingFunction
    {
        private readonly ILogger<TableInputBindingFunction> _logger;

        public TableInputBindingFunction(ILogger<TableInputBindingFunction> logger)
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
               
                await response.WriteStringAsync(text?.ToString() ?? "");
            }
            
            return response;
        }


        [Function(nameof(ReadTableDataFunction))]
        public async Task<HttpResponseData> ReadTableDataFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "ReadTableDataFunction/items/{partitionKey}/{rowKey}")] HttpRequestData req,
            [TableInput("TableName", "{partitionKey}", "{rowKey}")] TableEntity table)

        {
            table.TryGetValue("Text", out var text);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(text?.ToString() ?? "");
            return response;
        }

        [Function(nameof(ReadTableDataFunctionWithFilter))]
        public async Task<HttpResponseData> ReadTableDataFunctionWithFilter(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "ReadTableDataFunctionWithFilter/items/{partition}/{rowKey}")] HttpRequestData req,
            [TableInput("TableName", "{partition}", Filter = "RowKey ne '" + "{rowKey}'", Take =2, IsBatched = true)] IEnumerable<TableEntity> table)

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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "EnumerableFunction/items/{partitionKey}")] HttpRequestData req,
            [TableInput("TableName", "{partitionKey}", IsBatched = true)] IEnumerable<TableEntity> tables)

        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            List<string> tableList = new();
            foreach (TableEntity tableEntity in tables)
            {
                tableEntity.TryGetValue("Text", out var text);
                _logger.LogInformation("Value of text: " + text);
                tableList.Add(text?.ToString() ?? "");
            }
            await response.WriteStringAsync(string.Join(",", tableList));
            return response;
        }
    }
}

