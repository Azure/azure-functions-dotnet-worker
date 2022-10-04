// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public class ToDoItem
    {
        public string Id { get; set; }
        public string Description { get; set; }
    }

    public static class CosmosClientFunction
    {
        [Function(nameof(CosmosClientFunction))]
        public static async Task Run([CosmosDBTrigger(
            "testdb",
            "testcontainer",
            Connection = "CosmosDBConnection",
            CreateLeaseContainerIfNotExists = true)] IReadOnlyList<ToDoItem> todoItems,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(CosmosClientFunction));
            if (todoItems != null && todoItems.Count > 0)
            {
                logger.LogInformation($"Documents modified: {todoItems.Count}");
                logger.LogInformation($"First document Id: {todoItems[0].Id}");
                logger.LogInformation($"First document description: {todoItems[0].Description}");
            }
        }
    }
}
