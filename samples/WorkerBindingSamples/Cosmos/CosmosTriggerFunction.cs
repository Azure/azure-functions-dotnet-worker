// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    // We cannot use SDK-type bindings with the Cosmos trigger binding. There is no way for
    // the CosmosDB SDK to let us know the ID of the document that triggered the function;
    // therefore we cannot create a client that is able to pull the triggering document.
    public static class CosmosTriggerFunction
    {
        [Function(nameof(CosmosTriggerFunction))]
        public static void Run([CosmosDBTrigger(
            databaseName: "ToDoItems",
            containerName:"TriggerItems",
            Connection = "CosmosDBConnection",
            CreateLeaseContainerIfNotExists = true)] IReadOnlyList<ToDoItem> todoItems,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(CosmosTriggerFunction));

            if (todoItems is not null && todoItems.Any())
            {
                foreach (var doc in todoItems)
                {
                    logger.LogInformation("ToDoItem: {desc}", doc.Description);
                }
            }
        }
    }
}
