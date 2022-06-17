// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Extensions.CosmosDB
{
    //<docsnippet_qtrigger_with_cosmosdb_inputbinding>
    public class CosmosDbInputBindingFunction
    {
        private readonly ILogger _logger;

        public CosmosDbInputBindingFunction(ILogger<CosmosDbInputBindingFunction> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Function("DocByIdFromJSON")]
        public void Run(
            [QueueTrigger("todoqueueforlookup")] ToDoItemLookup toDoItemLookup,
            [CosmosDBInput(databaseName: "ToDoItems",
                           collectionName: "Items",
                           ConnectionStringSetting = "CosmosConnection",
                           Id ="{ToDoItemId}",
                           PartitionKey ="{ToDoItemPartitionKeyValue}")] ToDoItem toDoItem)
        {
            if (toDoItem == null)
            {
                _logger.LogInformation("ToDo item not found");
            }
            else
            {
                _logger.LogInformation($"Found ToDo item, Description={toDoItem.Description}");
            }
        }
    }

    public record ToDoItemLookup(string ToDoItemId, string ToDoItemPartitionKeyValue);
    //</docsnippet_qtrigger_with_cosmosdb_inputbinding>

    //<docsnippet_qtrigger_with_cosmosdb_inputbinding_todo_model>
    public class ToDoItem
    {
        public string Id { get; set; }
        public string PartitionKey { get; set; }
        public string Description { get; set; }
    }
    //<docsnippet_qtrigger_with_cosmosdb_inputbinding_todo_model>
}
