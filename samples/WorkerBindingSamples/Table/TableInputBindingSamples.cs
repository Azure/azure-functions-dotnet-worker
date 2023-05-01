// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net;
using System.Text;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
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

        [Function(nameof(TableInputClientFunction))]
        public Azure.Response TableInputClientFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [TableInput("TableName") ]  TableClient table)

        {
            var tableData = new MyTableData()
            {
                PartitionKey = "My Partition",
                RowKey = Guid.NewGuid().ToString(),
                Text = $"Output record created at {DateTime.Now}"
            };

            var entity = new TableEntity(tableData.PartitionKey, tableData.RowKey)
            {
                { "Text", "lit" }
            };
            return table.AddEntity(entity);
        }

        [Function(nameof(ReadTableDataFunction))]
        public void ReadTableDataFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [TableInput("TableName", "My Partition", "75961b28-aab8-494f-ae92-ae81697e4c1d")] TableEntity table)

        {
            table.TryGetValue("Text", out var text);
            _logger.LogInformation("Value of text: " + text);
        }

        [Function(nameof(EnumerableFunction))]
        public void EnumerableFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [TableInput("TableName", "My Partition", IsBatched = true)] IEnumerable<TableEntity> tables)

        {
            foreach (TableEntity tableEntity in tables)
            {
                tableEntity.TryGetValue("Text", out var text);
                _logger.LogInformation("Value of text: " + text);
            }
        }

        public class MyTableData
        {
            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public string Text { get; set; }
        }
    }
}

