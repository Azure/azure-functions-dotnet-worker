// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class TableFunction
    {
        [Function("TableFunction")]
        [TableOutput("OutputTable", Connection = "AzureWebJobsStorage")]
        public static MyTableData Run([QueueTrigger("table-items")] string input,
            [TableInput("MyTable", "MyPartition", "{queueTrigger}")] MyTableData tableInput,
            FunctionContext context)
        {
            var logger = context.GetLogger("TableFunction");

            logger.LogInformation($"PK={tableInput.PartitionKey}, RK={tableInput.RowKey}, Text={tableInput.Text}");

            return new MyTableData()
            {
                PartitionKey = "queue",
                RowKey = Guid.NewGuid().ToString(),
                Text = $"Output record with rowkey {input} created at {DateTime.Now}"
            };
        }
    }

    public class MyTableData
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string Text { get; set; }
    }
}
