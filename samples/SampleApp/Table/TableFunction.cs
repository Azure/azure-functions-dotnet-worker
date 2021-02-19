// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Storage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace SampleApp
{
    public static class TableFunction
    {
        [Function("TableFunction")]
        [TableOutput("output", "outputQueue", Connection = "ServiceBusConnection")]
        public static void Run([QueueTrigger("table-items")] string input,
            [TableInput("MyTable", "MyPartition", "{queueTrigger}")] JObject tableItem,
            FunctionContext context)
        {
            var logger = context.GetLogger("TableFunction");

            logger.LogInformation(tableItem.ToString());

            var message = $"Output message created at {DateTime.Now}";
            context.OutputBindings["output"] = new MyTableData()
            {
                PartitionKey = "queue",
                RowKey = Guid.NewGuid().ToString(),
                Text = message
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
