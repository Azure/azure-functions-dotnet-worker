// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public class TableFunction
    {
        private readonly ILogger<TableFunction> _logger;

        public TableFunction(ILogger<TableFunction> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// This function demonstrates binding to a single <see cref="TableClient"/>
        /// and using the client for output.
        /// </summary>
        [Function(nameof(TableFunction))]
        public async Task Run(
            [QueueTrigger("table-items")] string rowKey,
            [TableInput("MyTable")] TableClient table)
        {
            var tableEntities = table.QueryAsync<TableEntity>(filter: $"RowKey eq '{rowKey}'");

            await foreach (TableEntity entity in tableEntities)
            {
                _logger.LogInformation("PK={pk}, RK={rk}, Text={t}", entity.PartitionKey, entity.RowKey, entity["Text"]);
            }

            await table.AddEntityAsync(new TableEntity("processed", Guid.NewGuid().ToString())
            {
                ["Text"] = $"Record with row key {rowKey} processed at {DateTime.Now}"
            });
        }

        /// <summary>
        /// This function demonstrates binding to a single <see cref="TableEntity"/>, using the
        /// <see cref="TableInputAttribute.PartitionKey"/> and <see cref="TableInputAttribute.RowKey"/> properties.
        /// </summary>
        [Function(nameof(TableEntityFunction))]
        public void TableEntityFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "items/{partitionKey}/{rowKey}")] HttpRequestData req,
            [TableInput("MyTable", "{partitionKey}", "{rowKey}")] TableEntity entity)
        {
            _logger.LogInformation("PK={pk}, RK={rk}, Text={t}", entity.PartitionKey, entity.RowKey, entity["Text"]);
        }

        /// <summary>
        /// This function demonstrates binding to a collection of <see cref="TableEntity"/>, using the <see cref="TableInputAttribute.PartitionKey"/>.
        /// Note that when the <see cref="TableInputAttribute.RowKey"/> is not provided, you are able to bind to a collection.
        /// </summary>
        [Function(nameof(TableEntityCollectionFunction))]
        public void TableEntityCollectionFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "items/{partitionKey}")] HttpRequestData req,
            [TableInput("MyTable", "{partitionKey}")] IEnumerable<TableEntity> entities)
        {
            foreach (var entity in entities)
            {
                _logger.LogInformation("PK={pk}, RK={rk}, Text={t}", entity.PartitionKey, entity.RowKey, entity["Text"]);
            }
        }

        /// <summary>
        /// This function demonstrates binding to a collection of <see cref="TableEntity"/>, using <see cref="TableInputAttribute.Filter"/>
        /// to filter on the row key. This sample also demonstrates using <see cref="TableInputAttribute.Take"/>
        /// to limit the number of entities returned.
        /// </summary>
        [Function(nameof(TableEntityWithFilterFunction))]
        public void TableEntityWithFilterFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [TableInput("MyTable", "PartitionKey", 2, Filter = "RowKey ne 'value'")] IEnumerable<TableEntity> entities)
        {
            foreach (var entity in entities)
            {
                _logger.LogInformation("PK={pk}, RK={rk}, Text={t}", entity.PartitionKey, entity.RowKey, entity["Text"]);
            }
        }

        /// <summary>
        /// This function demonstrates binding to a collection of <see cref="IEnumerable{T}"/>
        /// </summary>
        [Function(nameof(TablePocoFunction))]
        public void TablePocoFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get","post", Route = null)] HttpRequestData req,
            [TableInput("MyTable")] IEnumerable<MyTableData> entities)
        {
            foreach (var entity in entities)
            {
                _logger.LogInformation("PK={pk}, RK={rk}, Text={t}", entity.PartitionKey, entity.RowKey, entity.Text);
            }
        }
    }

    public class MyTableData
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string Text { get; set; }
    }
}
