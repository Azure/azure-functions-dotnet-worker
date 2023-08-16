// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace WorkerBindingSamples.Table
{
    /// <summary>
    /// Samples demonstrating binding to <see cref="TableClient"/>, <see cref="TableEntity"/> and <see cref="IEnumerable{T}"/> types.
    /// </summary>
    public class TableSamples
    {
        private readonly ILogger<TableSamples> _logger;

        public TableSamples(ILogger<TableSamples> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// This function demonstrates binding to a single <see cref="TableClient"/>.
        /// </summary>
        [Function(nameof(TableClientFunction))]
        public async Task TableClientFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [TableInput("TableName")] TableClient table)
        {
            var tableEntity = table.QueryAsync<TableEntity>();

            await foreach (TableEntity entity in tableEntity)
            {
                _logger.LogInformation("PK={pk}, RK={rk}, Text={t}", entity.PartitionKey, entity.RowKey, entity["Text"]);
            }
        }

        /// <summary>
        /// This function demonstrates binding to a single <see cref="TableEntity"/>, using the
        /// <see cref="TableInputAttribute.PartitionKey"/> and <see cref="TableInputAttribute.RowKey"/> properties.
        /// </summary>
        [Function(nameof(TableEntityFunction))]
        public void TableEntityFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "items/{partitionKey}/{rowKey}")] HttpRequestData req,
            [TableInput("TableName", "{partitionKey}", "{rowKey}")] TableEntity entity)
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
            [TableInput("TableName", "{partitionKey}")] IEnumerable<TableEntity> entities)
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
            [TableInput("TableName", "PartitionKey", 2, Filter = "RowKey ne 'value'")] IEnumerable<TableEntity> entities)
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
            [TableInput("TableName")] IEnumerable<MyEntity> entities)
        {
            foreach (var entity in entities)
            {
                _logger.LogInformation("PK={pk}, RK={rk}, Text={t}", entity.PartitionKey, entity.RowKey, entity.Text);
            }
        }
    }

    public class MyEntity
    {
        public string? Text { get; set; }
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
    }
}
